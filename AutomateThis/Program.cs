using System.Diagnostics;
using System.Reflection;
using AutoMapper;
using AutomateThis.Providers.OpenWeatherMap;
using AutomateThis.Weather;
using Refit;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddYamlFile("appsettings.yaml");
var envConfig = $"appsettings.{builder.Environment.EnvironmentName}.yaml";
if (File.Exists(envConfig))
{
	builder.Configuration.AddYamlFile(envConfig);
}

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApiDocument();
builder.Services.AddAutoMapper(Assembly.GetExecutingAssembly());
builder.Services
	.AddRefitClient<IOpenWeatherMapApi>()
	.ConfigureHttpClient(c => c.BaseAddress = new Uri("https://api.openweathermap.org"));

var app = builder.Build();

app.UseOpenApi();
app.UseReDoc();

Weather? currentWeather = null;
app.MapGet("/weather", async (IOpenWeatherMapApi api, IMapper mapper) =>
{
	if (currentWeather is null || DateTimeOffset.Now - currentWeather.Timestamp >= TimeSpan.FromMinutes(30))
	{
		var response = await api.GetCurrentAsync(
			app.Configuration["OpenWeatherMap:Location"],
			"metric",
			app.Configuration["OpenWeatherMap:ApiKey"]
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
}

app.Run();
