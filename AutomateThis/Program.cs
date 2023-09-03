using System.Diagnostics;
using System.Reflection;
using AutoMapper;
using AutomateThis;
using AutomateThis.Providers.OpenWeatherMap;
using Microsoft.Extensions.Caching.Memory;
using Polly.Caching;
using Polly.Caching.Memory;
using Refit;
using Serilog.Events;
using Serilog;
using Stashbox;

Log.Logger = new LoggerConfiguration()
	.MinimumLevel.Override("Microsoft", LogEventLevel.Information)
	.MinimumLevel.Information()
	.Enrich.FromLogContext()
	.WriteTo.Console()
	.CreateBootstrapLogger();
Log.Logger.Information("Initializing application.");

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddYamlFile("appsettings.yaml");
var envConfig = $"appsettings.{builder.Environment.EnvironmentName}.yaml";
if (File.Exists(envConfig))
{
	Log.Logger
		.ForContext("EnvironmentConfig", envConfig)
		.Information("Loading environment config {EnvironmentConfig}");
	builder.Configuration.AddYamlFile(envConfig);
}

var servicesContainer = new StashboxContainer(config =>
{
	config
		.WithLifetimeValidation();
});
builder.Host.UseStashbox(servicesContainer);
builder.Host.ConfigureContainer<IStashboxContainer>((ctx, container) =>
{
	if (ctx.HostingEnvironment.IsDevelopment())
	{
		try
		{
			container.Validate();
		}
		catch
		{
			var diagnostics = container.GetRegistrationDiagnostics();
			foreach(var diagnostic in diagnostics.OrderBy(d => d.ServiceType.Name))
			{
				Debug.WriteLine(diagnostic);
			}
			throw;
		}
	}
});

var memoryCache = new MemoryCache(new MemoryCacheOptions());
var memoryCacheProvider = new MemoryCacheProvider(memoryCache);
servicesContainer.RegisterInstance(memoryCacheProvider);
servicesContainer.RegisterInstance<IAsyncCacheProvider>(memoryCacheProvider);
servicesContainer.AddRegistry();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApiDocument();
builder.Services.AddAutoMapper(Assembly.GetExecutingAssembly());
builder.Services
	.AddRefitClient<IOpenWeatherMapApi>()
	.AddPolicyHandlerFromRegistry(Policies.OpenWeatherMap)
	.ConfigureHttpClient(c => c.BaseAddress = new Uri("https://api.openweathermap.org"));
builder.Services.AddControllers().AddControllersAsServices();
builder.Services.AddSingleton(builder.Configuration);

builder.Host.UseSerilog(
	(context, services, configuration) => configuration
		.ReadFrom.Configuration(context.Configuration)
		.ReadFrom.Services(services)
		.Enrich.FromLogContext()
		.MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
);

var app = builder.Build();
Log.Logger.Debug("Build successful.");

app.UseSerilogRequestLogging();
app.UseOpenApi();
app.UseReDoc();
app.MapControllers();

if (app.Environment.IsDevelopment())
{
	var mapper = app.Services.GetService<IMapper>();
	Debug.Assert(mapper is not null);
	mapper.ConfigurationProvider.AssertConfigurationIsValid();
	Log.Logger.Information("AutoMapper configuration is valid.");
}

Log.Logger.Information("Initialization complete. Running application.");
try
{
	app.Run();
}
finally
{
	await Log.CloseAndFlushAsync();
}
