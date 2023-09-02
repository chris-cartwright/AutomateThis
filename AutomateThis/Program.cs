using System.Diagnostics;
using System.Reflection;
using AutoMapper;
using AutomateThis;
using AutomateThis.Providers.OpenWeatherMap;
using AutomateThis.Weather;
using Microsoft.Extensions.Caching.Memory;
using Polly;
using Polly.Caching;
using Polly.Caching.Memory;
using Polly.Registry;
using Refit;
using Serilog.Events;
using Serilog;

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

var memoryCache = new MemoryCache(new MemoryCacheOptions());
var memoryCacheProvider = new MemoryCacheProvider(memoryCache);
builder.Services.AddSingleton(memoryCacheProvider);
builder.Services.AddSingleton<IAsyncCacheProvider>(memoryCacheProvider);
builder.Services.AddRegistry();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApiDocument();
builder.Services.AddAutoMapper(Assembly.GetExecutingAssembly());
builder.Services
	.AddRefitClient<IOpenWeatherMapApi>()
	.AddPolicyHandlerFromRegistry(Policies.OpenWeatherMap)
	.ConfigureHttpClient(c => c.BaseAddress = new Uri("https://api.openweathermap.org"));

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

app.MapGet("/weather", async (IOpenWeatherMapApi api, IMapper mapper, IReadOnlyPolicyRegistry<string> policies) =>
{
	return await policies.Get<IAsyncPolicy<Weather>>(Policies.Weather).ExecuteAsync(async () =>
	{
		var response = await api.GetCurrentAsync(
			app.Configuration["OpenWeatherMap:Location"] ?? throw new ArgumentNullException(),
			"metric",
			app.Configuration["OpenWeatherMap:ApiKey"] ?? throw new ArgumentNullException()
		);
		return mapper.Map<Weather>(response.Content);
	});
});

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
