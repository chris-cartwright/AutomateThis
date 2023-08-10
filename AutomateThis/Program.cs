using Refit;
using AutomateThis.OpenWeatherMap;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services
    .AddRefitClient<IOpenWeatherMap>()
    .ConfigureHttpClient(c => c.BaseAddress = new Uri("https://api.openweathermap.org"));

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

Weather? currentWeather = null;
app.MapGet("/weather", async (IOpenWeatherMap api) =>
{
    if (currentWeather is null || DateTimeOffset.Now - currentWeather.Value.Timestamp >= TimeSpan.FromMinutes(30))
    {
        var response = await api.GetCurrentAsync("Winnipeg,Manitoba", "metric", "*****");
        currentWeather = response.Content;
    }

    return currentWeather;
});

app.Run();
