using System.Buffers.Text;
using Refit;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AutomateThis.OpenWeatherMap;

public struct Coordinates
{
    [JsonPropertyName("lat")]
    public float Latitude;
    [JsonPropertyName("lon")]
    public float Longitude;
}

public struct Conditions
{
    public int Id;
    public string Main;
    public string Description;
    public string Icon;
}

public struct MainInfo
{
    [JsonPropertyName("temp")]
    public float Temperature;
    public float FeelsLike;
    [JsonPropertyName("temp_min")]
    public float MinTemperature;
    [JsonPropertyName("temp_max")]
    public float MaxTemperature;
    public int Pressure;
    public int Humidity;
}

public struct Wind
{
    public float Speed;
    [JsonPropertyName("deg")]
    public int Degrees;
    [JsonPropertyName("gust")]
    public float Gusts;
}

public struct Clouds
{
    public int All; // 0-100
}

public struct Precipitation
{
    [JsonPropertyName("1h")]
    public int OneHour;
    [JsonPropertyName("3h")]
    public int ThreeHours;
}

public struct LocationInfo
{
    public string Country;
    [JsonConverter(typeof(UnixEpochConverter))]
    public DateTimeOffset Sunrise;
    [JsonConverter(typeof(UnixEpochConverter))]
    public DateTimeOffset Sunset;
}

public struct Weather
{
    [JsonPropertyName("coord")]
    public Coordinates Coordinates;
    [JsonPropertyName("weather")]
    public List<Conditions> Conditions;
    public MainInfo Main;
    public int Visibility;
    public Wind Wind;
    public Clouds Clouds;
    public Precipitation? Rain;
    public Precipitation? Snow;
    [JsonPropertyName("dt")]
    [JsonConverter(typeof(UnixEpochConverter))]
    public DateTimeOffset Timestamp;
    [JsonPropertyName("sys")]
    public LocationInfo LocationInfo;
}

public class UnixEpochConverter : JsonConverter<DateTimeOffset>
{
    public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return DateTimeOffset.FromUnixTimeSeconds(reader.GetUInt32());
    }

    public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}

public interface IOpenWeatherMap
{
    // https://api.openweathermap.org/data/2.5/weather?q=Winnipeg,Manitoba&units=metric&appid=*****
    [Get("/data/2.5/weather")]
    Task<ApiResponse<Weather>> GetCurrentAsync([AliasAs("q")] string location, string units, string appId);
}
