using Google.Cloud.Firestore;

namespace LawncareApi.Models;

/// <summary>
/// Daily forecast entry returned to clients.
/// </summary>
public class DailyForecast
{
    public string Date { get; set; } = string.Empty;
    public double TempMaxF { get; set; }
    public double TempMinF { get; set; }

    /// <summary>Simplified weather code for icon mapping on ESP32 display.</summary>
    public int WeatherCode { get; set; }

    /// <summary>Human-readable condition label from NWS shortForecast.</summary>
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

/// <summary>Firestore cache for NWS grid point lookups (grid office + X,Y don't change for a location).</summary>
[FirestoreData]
public class NwsGridPointCache
{
    [FirestoreDocumentId]
    public string? Id { get; set; }

    [FirestoreProperty]
    public string ForecastUrl { get; set; } = string.Empty;

    [FirestoreProperty]
    public DateTime CachedAt { get; set; }
}

// ── NWS API response models ────────────────────────────────────────────

public class NwsPointsResponse
{
    public NwsPointsProperties? Properties { get; set; }
}

public class NwsPointsProperties
{
    public string Forecast { get; set; } = string.Empty;
}

public class NwsForecastResponse
{
    public NwsForecastProperties? Properties { get; set; }
}

public class NwsForecastProperties
{
    public IList<NwsForecastPeriod> Periods { get; set; } = [];
}

public class NwsForecastPeriod
{
    public int Number { get; set; }
    public string Name { get; set; } = string.Empty;
    public string StartTime { get; set; } = string.Empty;
    public bool IsDaytime { get; set; }
    public int Temperature { get; set; }
    public string TemperatureUnit { get; set; } = "F";
    public NwsPrecipitation? ProbabilityOfPrecipitation { get; set; }
    public string WindSpeed { get; set; } = string.Empty;
    public string ShortForecast { get; set; } = string.Empty;
}

public class NwsPrecipitation
{
    public int? Value { get; set; }
}
