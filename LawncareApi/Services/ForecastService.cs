using System.Text.Json;
using Google.Cloud.Firestore;
using LawncareApi.Models;

namespace LawncareApi.Services;

public interface IForecastService
{
    /// <summary>
    /// Returns up to 7 days of forecast data for the given coordinates.
    /// Results are cached in Firestore for <paramref name="cacheDurationMinutes"/> minutes.
    /// </summary>
    Task<WeatherForecastResponse> GetForecastAsync(
        double lat, double lon, int cacheDurationMinutes = 60, CancellationToken ct = default);
}

public class ForecastService : IForecastService
{
    private readonly FirestoreDb _db;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ForecastService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public ForecastService(FirestoreDb db, IHttpClientFactory httpClientFactory, ILogger<ForecastService> logger)
    {
        _db = db;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<WeatherForecastResponse> GetForecastAsync(
        double lat, double lon, int cacheDurationMinutes = 60, CancellationToken ct = default)
    {
        // Round coordinates to 2 decimal places for cache key stability
        var cacheKey = $"{Math.Round(lat, 2)}_{Math.Round(lon, 2)}";
        var cacheDoc = _db.Collection("forecast_cache").Document(cacheKey);

        // Check cache first
        var snapshot = await cacheDoc.GetSnapshotAsync(ct);
        if (snapshot.Exists)
        {
            var cached = snapshot.ConvertTo<ForecastCacheEntry>();
            if ((DateTime.UtcNow - cached.CachedAt).TotalMinutes < cacheDurationMinutes)
            {
                _logger.LogDebug("Forecast cache hit for {Key}", cacheKey);
                var cachedDaily = JsonSerializer.Deserialize<List<DailyForecast>>(cached.DailyJson, JsonOptions) ?? [];
                return BuildResponse(cachedDaily);
            }
        }

        // Fetch from Open-Meteo
        var daily = await FetchFromOpenMeteoAsync(lat, lon, ct);

        // Store in cache
        var entry = new ForecastCacheEntry
        {
            DailyJson = JsonSerializer.Serialize(daily),
            CachedAt = DateTime.UtcNow,
        };
        await cacheDoc.SetAsync(entry, cancellationToken: ct);

        _logger.LogInformation("Forecast fetched and cached for {Key} ({Count} days)", cacheKey, daily.Count);
        return BuildResponse(daily);
    }

    private async Task<List<DailyForecast>> FetchFromOpenMeteoAsync(
        double lat, double lon, CancellationToken ct)
    {
        var client = _httpClientFactory.CreateClient();
        var url = $"https://api.open-meteo.com/v1/forecast?latitude={lat}&longitude={lon}" +
                  "&daily=temperature_2m_max,temperature_2m_min,weather_code," +
                  "precipitation_probability_max,precipitation_sum,wind_speed_10m_max" +
                  "&temperature_unit=fahrenheit&wind_speed_unit=kmh" +
                  "&timezone=America%2FNew_York&forecast_days=7";

        var json = await client.GetStringAsync(url, ct);
        var resp = JsonSerializer.Deserialize<OpenMeteoForecastResponse>(json, JsonOptions);

        if (resp?.Daily is null || resp.Daily.Time.Count == 0)
            return [];

        var d = resp.Daily;
        var results = new List<DailyForecast>();
        for (var i = 0; i < d.Time.Count; i++)
        {
            var code = i < d.Weather_code.Count ? d.Weather_code[i] : 0;
            results.Add(new DailyForecast
            {
                Date = d.Time[i],
                TempMaxF = i < d.Temperature_2m_max.Count ? d.Temperature_2m_max[i] : 0,
                TempMinF = i < d.Temperature_2m_min.Count ? d.Temperature_2m_min[i] : 0,
                WeatherCode = code,
                Condition = WmoCodeToCondition(code),
                Icon = WmoCodeToIcon(code),
                PrecipitationProbabilityPct = i < d.Precipitation_probability_max.Count
                    ? d.Precipitation_probability_max[i] : 0,
                PrecipitationMm = i < d.Precipitation_sum.Count ? d.Precipitation_sum[i] : 0,
                WindMaxKmh = i < d.Wind_speed_10m_max.Count ? d.Wind_speed_10m_max[i] : 0,
            });
        }

        return results;
    }

    private static WeatherForecastResponse BuildResponse(List<DailyForecast> daily)
    {
        var today = DateTime.UtcNow.ToString("yyyy-MM-dd");
        return new WeatherForecastResponse
        {
            Today = daily.Find(d => d.Date == today),
            Daily = daily,
            CachedAt = DateTime.UtcNow,
        };
    }

    /// <summary>Maps WMO 4677 weather code to a human-readable condition string.</summary>
    internal static string WmoCodeToCondition(int code) => code switch
    {
        0 => "Clear sky",
        1 => "Mainly clear",
        2 => "Partly cloudy",
        3 => "Overcast",
        45 or 48 => "Fog",
        51 => "Light drizzle",
        53 => "Moderate drizzle",
        55 => "Dense drizzle",
        56 or 57 => "Freezing drizzle",
        61 => "Slight rain",
        63 => "Moderate rain",
        65 => "Heavy rain",
        66 or 67 => "Freezing rain",
        71 => "Slight snow",
        73 => "Moderate snow",
        75 => "Heavy snow",
        77 => "Snow grains",
        80 => "Slight rain showers",
        81 => "Moderate rain showers",
        82 => "Violent rain showers",
        85 => "Slight snow showers",
        86 => "Heavy snow showers",
        95 => "Thunderstorm",
        96 or 99 => "Thunderstorm with hail",
        _ => "Unknown",
    };

    /// <summary>Maps WMO 4677 weather code to a Material icon name.</summary>
    internal static string WmoCodeToIcon(int code) => code switch
    {
        0 => "wb_sunny",
        1 or 2 => "partly_cloudy_day",
        3 => "cloud",
        45 or 48 => "foggy",
        51 or 53 or 55 or 56 or 57 => "grain",
        61 or 63 or 80 or 81 => "rainy",
        65 or 82 => "thunderstorm",
        66 or 67 => "ac_unit",
        71 or 73 or 75 or 77 or 85 or 86 => "cloudy_snowing",
        95 or 96 or 99 => "thunderstorm",
        _ => "cloud",
    };
}
