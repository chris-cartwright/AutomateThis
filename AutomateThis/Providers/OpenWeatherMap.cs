using Refit;
using System.Text.Json;
using System.Text.Json.Serialization;
using AutoMapper;
using AutomateThis.Weather;

namespace AutomateThis.Providers.OpenWeatherMap;

public class Coordinates
{
	[JsonPropertyName("lat")]
	public float Latitude { get; set; }
	[JsonPropertyName("lon")]
	public float Longitude { get; set; }
}

public class Conditions
{
	public int Id { get; set; }
	public string Main { get; set; }
	public string Description { get; set; }
	public string Icon { get; set; }
}

public class MainInfo
{
	[JsonPropertyName("temp")]
	public float Temperature { get; set; }
	[JsonPropertyName("feels_like")]
	public float FeelsLike { get; set; }
	[JsonPropertyName("temp_min")]
	public float MinTemperature { get; set; }
	[JsonPropertyName("temp_max")]
	public float MaxTemperature { get; set; }
	public int Pressure { get; set; }
	public int Humidity { get; set; }
}

public class Wind
{
	public float Speed { get; set; }
	[JsonPropertyName("deg")]
	public int Degrees { get; set; }
	[JsonPropertyName("gust")]
	public float Gusts { get; set; }
}

public class Clouds
{
	public int All { get; set; } // 0-100
}

public class Precipitation
{
	[JsonPropertyName("1h")]
	public int OneHour { get; set; }
	[JsonPropertyName("3h")]
	public int ThreeHours { get; set; }
}

public class LocationInfo
{
	public string Country { get; set; }
	[JsonConverter(typeof(UnixEpochConverter))]
	public DateTimeOffset Sunrise { get; set; }
	[JsonConverter(typeof(UnixEpochConverter))]
	public DateTimeOffset Sunset { get; set; }
}

public class OpenWeather
{
	[JsonPropertyName("coord")]
	public Coordinates Coordinates { get; set; }
	[JsonPropertyName("weather")]
	public List<Conditions> Conditions { get; set; }
	public MainInfo Main { get; set; }
	public int Visibility { get; set; }
	public Wind Wind { get; set; }
	public Clouds Clouds { get; set; }
	public Precipitation? Rain { get; set; }
	public Precipitation? Snow { get; set; }
	[JsonPropertyName("dt")]
	[JsonConverter(typeof(UnixEpochConverter))]
	public DateTimeOffset Timestamp { get; set; }
	[JsonPropertyName("sys")]
	public LocationInfo LocationInfo { get; set; }
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

public interface IOpenWeatherMapApi
{
	// https://api.openweathermap.org/data/2.5/weather?q=Winnipeg,Manitoba&units=metric&appid=*****
	[Get("/data/2.5/weather")]
	Task<ApiResponse<OpenWeather>> GetCurrentAsync([AliasAs("q")] string location, string units, string appid);
}

public class MappingProfile : Profile
{
	public MappingProfile()
	{
		CreateMap<OpenWeather, Weather.Weather>()
			.ForMember(dst => dst.CloudCoverage, opt => opt.MapFrom((ow, _) => ow.Clouds.All / 100))
			.ForMember(dst => dst.Sunrise, opt => opt.MapFrom(src => src.LocationInfo.Sunrise))
			.ForMember(dst => dst.Sunset, opt => opt.MapFrom(src => src.LocationInfo.Sunset))
			.ForMember(dst => dst.Temperature, opt => opt.MapFrom(src => src.Main))
			.ForMember(dst => dst.Humidity, opt => opt.MapFrom(src => src.Main.Humidity))
			.ForMember(dst => dst.Pressure, opt => opt.MapFrom(src => src.Main.Pressure));
		CreateMap<MainInfo, Temperature>()
			.ForMember(dst => dst.Current, opt => opt.MapFrom(src => src.Temperature))
			.ForMember(dst => dst.Min, opt => opt.MapFrom(src => src.MinTemperature))
			.ForMember(dst => dst.Max, opt => opt.MapFrom(src => src.MaxTemperature));
		CreateMap<Conditions, Weather.Conditions>();
		CreateMap<Wind, Weather.Wind>();
		CreateMap<Precipitation, Weather.Precipitation>();
	}
}
