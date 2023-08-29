using System.Text.Json.Serialization;

namespace AutomateThis.Weather;

public class Wind
{
	/// <summary>
	/// km/h
	/// </summary>
	public float Speed { get; set; }

	public int Degrees { get; set; }

	/// <summary>
	/// km/h
	/// </summary>
	public float Gusts { get; set; }
}

public class Precipitation
{
	[JsonPropertyName("1h")]
	public int OneHour { get; set; }
	[JsonPropertyName("3h")]
	public int ThreeHours { get; set; }
}

public class Temperature
{
	public float Current { get; set; }
	public float Min { get; set; }
	public float Max { get; set; }
	public float FeelsLike { get; set; }
}

public class Conditions
{
	/// <summary>
	/// Brief summary of this condition.
	/// </summary>
	public string Main { get; set; } = "";

	/// <summary>
	/// Additional details. Free form.
	/// </summary>
	public string? Description { get; set; }
}

public class Weather
{
	public List<Conditions> Conditions { get; set; } = new();
	public Wind Wind { get; set; } = new();

	/// <summary>
	/// As a percentage.
	/// </summary>
	public float CloudCoverage { get; set; }
	public Precipitation? Rain { get; set; }
	public Precipitation? Snow { get; set; }
	public DateTimeOffset Timestamp { get; set; }
	public DateTimeOffset Sunrise { get; set; }
	public DateTimeOffset Sunset { get; set; }
	
	/// <summary>
	/// As a distance in kilometers.
	/// </summary>
	public int Visibility { get; set; }

	/// <summary>
	/// In Celsius.
	/// </summary>
	public Temperature Temperature { get; set; } = new();

	/// <summary>
	/// 0 - 100
	/// </summary>
	public int Humidity { get; set; }

	/// <summary>
	/// hPa
	/// </summary>
	public int Pressure { get; set; }
}
