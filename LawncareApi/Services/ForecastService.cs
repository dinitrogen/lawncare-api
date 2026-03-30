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

        // Fetch from NWS
        var daily = await FetchFromNwsAsync(lat, lon, ct);

        // Store in cache
        var entry = new ForecastCacheEntry
        {
            DailyJson = JsonSerializer.Serialize(daily),
            CachedAt = DateTime.UtcNow,
        };
        await cacheDoc.SetAsync(entry, cancellationToken: ct);

        _logger.LogInformation("Forecast fetched from NWS and cached for {Key} ({Count} days)", cacheKey, daily.Count);
        return BuildResponse(daily);
    }

    /// <summary>
    /// Resolves lat/lon → NWS grid forecast URL (cached indefinitely since grid points don't change).
    /// </summary>
    private async Task<string> GetNwsForecastUrlAsync(double lat, double lon, CancellationToken ct)
    {
        var cacheKey = $"{Math.Round(lat, 2)}_{Math.Round(lon, 2)}";
        var cacheDoc = _db.Collection("nws_grid_cache").Document(cacheKey);

        var snapshot = await cacheDoc.GetSnapshotAsync(ct);
        if (snapshot.Exists)
        {
            var cached = snapshot.ConvertTo<NwsGridPointCache>();
            if (!string.IsNullOrEmpty(cached.ForecastUrl))
                return cached.ForecastUrl;
        }

        var client = CreateNwsClient();
        var json = await client.GetStringAsync($"https://api.weather.gov/points/{lat},{lon}", ct);
        var points = JsonSerializer.Deserialize<NwsPointsResponse>(json, JsonOptions);
        var forecastUrl = points?.Properties?.Forecast
            ?? throw new InvalidOperationException($"NWS /points returned no forecast URL for {lat},{lon}");

        await cacheDoc.SetAsync(new NwsGridPointCache
        {
            ForecastUrl = forecastUrl,
            CachedAt = DateTime.UtcNow,
        }, cancellationToken: ct);

        _logger.LogInformation("NWS grid point resolved: {Url}", forecastUrl);
        return forecastUrl;
    }

    private async Task<List<DailyForecast>> FetchFromNwsAsync(
        double lat, double lon, CancellationToken ct)
    {
        var forecastUrl = await GetNwsForecastUrlAsync(lat, lon, ct);
        var client = CreateNwsClient();
        var json = await client.GetStringAsync(forecastUrl, ct);
        var resp = JsonSerializer.Deserialize<NwsForecastResponse>(json, JsonOptions);

        var periods = resp?.Properties?.Periods;
        if (periods is null || periods.Count == 0)
            return [];

        // NWS returns 14 periods: alternating day/night for 7 days.
        // Pair them to extract daily high/low.
        var results = new List<DailyForecast>();
        var i = 0;

        while (i < periods.Count && results.Count < 7)
        {
            var period = periods[i];
            var date = DateOnly.Parse(period.StartTime[..10]).ToString("yyyy-MM-dd");

            int high, low;
            string condition;
            int precipPct;
            string windSpeed;

            if (period.IsDaytime)
            {
                high = period.Temperature;
                condition = period.ShortForecast;
                precipPct = period.ProbabilityOfPrecipitation?.Value ?? 0;
                windSpeed = period.WindSpeed;

                // Next period should be the night
                if (i + 1 < periods.Count && !periods[i + 1].IsDaytime)
                {
                    low = periods[i + 1].Temperature;
                    i += 2;
                }
                else
                {
                    low = high - 15; // fallback estimate
                    i++;
                }
            }
            else
            {
                // Started with a night period (partial first day)
                low = period.Temperature;
                precipPct = period.ProbabilityOfPrecipitation?.Value ?? 0;
                windSpeed = period.WindSpeed;
                condition = period.ShortForecast;
                high = low + 15; // fallback estimate
                i++;
            }

            var weatherCode = ConditionToWeatherCode(condition);

            results.Add(new DailyForecast
            {
                Date = date,
                TempMaxF = high,
                TempMinF = low,
                WeatherCode = weatherCode,
                Condition = SimplifyCondition(condition),
                Icon = WeatherCodeToIcon(weatherCode),
                PrecipitationProbabilityPct = precipPct,
                PrecipitationMm = 0, // NWS doesn't provide a simple daily mm sum
                WindMaxKmh = ParseWindSpeedToKmh(windSpeed),
            });
        }

        return results;
    }

    private HttpClient CreateNwsClient()
    {
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.UserAgent.ParseAdd("LawncareApp/1.0 (lawncare-7fa77.web.app)");
        client.DefaultRequestHeaders.Accept.ParseAdd("application/geo+json");
        return client;
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

    /// <summary>Extracts the first numeric mph value and converts to km/h.</summary>
    internal static double ParseWindSpeedToKmh(string windSpeed)
    {
        // "16 mph" or "10 to 18 mph" — take the higher value
        var parts = windSpeed.Split(' ');
        double maxMph = 0;
        foreach (var part in parts)
        {
            if (double.TryParse(part, out var mph) && mph > maxMph)
                maxMph = mph;
        }
        return Math.Round(maxMph * 1.60934, 1);
    }

    /// <summary>
    /// Maps NWS shortForecast text to a simplified weather code compatible with the ESP32 display icons.
    /// Codes loosely follow WMO 4677 groupings used by the existing icon drawing code.
    /// </summary>
    internal static int ConditionToWeatherCode(string condition)
    {
        var lower = condition.ToLowerInvariant();

        if (lower.Contains("thunder"))      return 95;
        if (lower.Contains("freezing rain") || lower.Contains("ice") || lower.Contains("sleet"))
                                             return 66;
        if (lower.Contains("snow") || lower.Contains("flurr") || lower.Contains("blizzard"))
                                             return 73;
        if (lower.Contains("heavy rain"))    return 65;
        if (lower.Contains("rain shower"))   return 80;
        if (lower.Contains("rain") || lower.Contains("showers"))
                                             return 61;
        if (lower.Contains("drizzle"))       return 51;
        if (lower.Contains("fog") || lower.Contains("mist") || lower.Contains("haze"))
                                             return 45;
        if (lower.Contains("overcast"))      return 3;
        if (lower.Contains("mostly cloudy") || lower.Contains("considerable cloud"))
                                             return 3;
        if (lower.Contains("partly"))        return 2;
        if (lower.Contains("mostly sunny") || lower.Contains("mostly clear"))
                                             return 1;
        if (lower.Contains("sunny") || lower.Contains("clear"))
                                             return 0;
        if (lower.Contains("cloud"))         return 3;

        return 3; // default to overcast
    }

    /// <summary>Simplify long NWS shortForecast strings for display.</summary>
    internal static string SimplifyCondition(string condition)
    {
        // NWS often has "Partly Sunny then Slight Chance Showers And Thunderstorms"
        // Take just the first clause if there's a "then"
        var thenIdx = condition.IndexOf(" then ", StringComparison.OrdinalIgnoreCase);
        var simplified = thenIdx > 0 ? condition[..thenIdx] : condition;

        // Trim common NWS probability prefixes for cleaner display
        simplified = simplified
            .Replace("Slight Chance ", "", StringComparison.OrdinalIgnoreCase)
            .Replace("Chance ", "", StringComparison.OrdinalIgnoreCase)
            .Replace("Likely ", "", StringComparison.OrdinalIgnoreCase)
            .Replace("Areas Of ", "", StringComparison.OrdinalIgnoreCase)
            .Replace("Patchy ", "", StringComparison.OrdinalIgnoreCase)
            .Trim();

        // Cap length for ESP32 display (24 char limit in ForecastDay.condition)
        return simplified.Length > 23 ? simplified[..23] : simplified;
    }

    /// <summary>Maps simplified weather code to a Material icon name.</summary>
    internal static string WeatherCodeToIcon(int code) => code switch
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
