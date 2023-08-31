using System.Diagnostics;
using System.Reflection;
using AutoMapper;
using AutomateThis.Providers.OpenWeatherMap;
using AutomateThis.Weather;
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

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApiDocument();
builder.Services.AddAutoMapper(Assembly.GetExecutingAssembly());
builder.Services
	.AddRefitClient<IOpenWeatherMapApi>()
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

Weather? currentWeather = null;
app.MapGet("/weather", async (IOpenWeatherMapApi api, IMapper mapper) =>
{
	if (currentWeather is null || DateTimeOffset.Now - currentWeather.Timestamp >= TimeSpan.FromMinutes(30))
	{
		var response = await api.GetCurrentAsync(
			app.Configuration["OpenWeatherMap:Location"] ?? throw new ArgumentNullException(),
			"metric",
			app.Configuration["OpenWeatherMap:ApiKey"] ?? throw new ArgumentNullException()
		);
		currentWeather = mapper.Map<Weather>(response.Content);
	}

	return currentWeather;
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
