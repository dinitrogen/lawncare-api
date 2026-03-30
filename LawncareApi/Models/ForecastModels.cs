using Google.Cloud.Firestore;

namespace LawncareApi.Models;

/// <summary>
/// Daily forecast entry returned to clients.
/// Weather codes follow the WMO 4677 standard used by Open-Meteo.
/// </summary>
public class DailyForecast
{
    public string Date { get; set; } = string.Empty;
    public double TempMaxF { get; set; }
    public double TempMinF { get; set; }

    /// <summary>WMO 4677 weather code. See https://open-meteo.com/en/docs#weathervariables</summary>
    public int WeatherCode { get; set; }

    /// <summary>Human-readable condition label derived from the WMO code.</summary>
    public string Condition { get; set; } = string.Empty;

    /// <summary>Material icon name suitable for Angular Material or ESP display mapping.</summary>
    public string Icon { get; set; } = string.Empty;

    public int PrecipitationProbabilityPct { get; set; }
    public double PrecipitationMm { get; set; }
    public double WindMaxKmh { get; set; }
}

/// <summary>
/// Combined current conditions + forecast response for the ESP display and web app.
/// </summary>
public class WeatherForecastResponse
{
    public DailyForecast? Today { get; set; }
    public IList<DailyForecast> Daily { get; set; } = [];
    public DateTime CachedAt { get; set; }
}

/// <summary>Firestore cache document for forecast data.</summary>
[FirestoreData]
public class ForecastCacheEntry
{
    [FirestoreDocumentId]
    public string? Id { get; set; }

    [FirestoreProperty]
    public string DailyJson { get; set; } = "[]";

    [FirestoreProperty]
    public DateTime CachedAt { get; set; }
}

/// <summary>Open-Meteo forecast response shape for daily data.</summary>
public class OpenMeteoForecastResponse
{
    public OpenMeteoForecastDaily? Daily { get; set; }
}

public class OpenMeteoForecastDaily
{
    public IList<string> Time { get; set; } = [];
    public IList<double> Temperature_2m_max { get; set; } = [];
    public IList<double> Temperature_2m_min { get; set; } = [];
    public IList<int> Weather_code { get; set; } = [];
    public IList<int> Precipitation_probability_max { get; set; } = [];
    public IList<double> Precipitation_sum { get; set; } = [];
    public IList<double> Wind_speed_10m_max { get; set; } = [];
}
