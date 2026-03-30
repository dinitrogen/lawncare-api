using System.Text.Json;
using Google.Cloud.Firestore;
using LawncareApi.Models;

namespace LawncareApi.Services;

public interface IGddApiService
{
    Task<IReadOnlyList<DailyGddEntry>> GetGddDataAsync(string uid, CancellationToken ct = default);
}

public class GddApiService : IGddApiService
{
    private readonly FirestoreDb _db;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IWeatherService _weatherService;
    private readonly ILogger<GddApiService> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public GddApiService(
        FirestoreDb db,
        IHttpClientFactory httpClientFactory,
        IWeatherService weatherService,
        ILogger<GddApiService> logger)
    {
        _db = db;
        _httpClientFactory = httpClientFactory;
        _weatherService = weatherService;
        _logger = logger;
    }

    public async Task<IReadOnlyList<DailyGddEntry>> GetGddDataAsync(string uid, CancellationToken ct = default)
    {
        var userDoc = await _db.Collection("users").Document(uid).GetSnapshotAsync(ct);
        if (!userDoc.Exists) throw new InvalidOperationException("User profile not found.");

        var user = userDoc.ConvertTo<AppUser>();

        var year = DateTime.UtcNow.Year;
        var startDate = $"{year}-{user.GddStartMonth:D2}-{user.GddStartDay:D2}";
        var endDate = DateTime.UtcNow.ToString("yyyy-MM-dd");

        IReadOnlyList<DailyGddEntry> entries;

        if (string.Equals(user.GddSource, "ecowitt", StringComparison.OrdinalIgnoreCase))
        {
            entries = await CalculateGddFromEcowittAsync(startDate, endDate, user.GddBase, user.TempOffset, ct);
        }
        else
        {
            double lat, lon;
            if (user.Latitude.HasValue && user.Longitude.HasValue)
            {
                lat = user.Latitude.Value;
                lon = user.Longitude.Value;
            }
            else
            {
                var geo = await GeocodeZipAsync(user.ZipCode, ct);
                lat = geo.Lat;
                lon = geo.Lon;
            }

            entries = await FetchAndCalculateGdd(lat, lon, startDate, endDate, user.GddBase, user.TempOffset, ct);
        }

        // Cache the results
        await CacheGddDataAsync(uid, year, entries, ct);

        return entries;
    }

    private async Task<IReadOnlyList<DailyGddEntry>> FetchAndCalculateGdd(
        double lat, double lon, string startDate, string endDate,
        int baseTempF, int tempOffset, CancellationToken ct)
    {
        var client = _httpClientFactory.CreateClient();
        var cutoff = DateTime.UtcNow.AddDays(-6).ToString("yyyy-MM-dd");

        var allTimes = new List<string>();
        var allMax = new List<double>();
        var allMin = new List<double>();

        // Historical data (ERA5 reanalysis)
        if (string.Compare(startDate, cutoff, StringComparison.Ordinal) <= 0)
        {
            var histEnd = string.Compare(endDate, cutoff, StringComparison.Ordinal) <= 0 ? endDate : cutoff;
            var url = $"https://archive-api.open-meteo.com/v1/archive?latitude={lat}&longitude={lon}" +
                      $"&daily=temperature_2m_max,temperature_2m_min" +
                      $"&temperature_unit=fahrenheit" +
                      $"&start_date={startDate}&end_date={histEnd}" +
                      $"&timezone=America%2FNew_York";
            try
            {
                var json = await client.GetStringAsync(url, ct);
                var resp = JsonSerializer.Deserialize<OpenMeteoResponse>(json, JsonOptions);
                if (resp?.Daily is not null)
                {
                    allTimes.AddRange(resp.Daily.Time);
                    allMax.AddRange(resp.Daily.Temperature_2m_max);
                    allMin.AddRange(resp.Daily.Temperature_2m_min);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch historical weather data from Open-Meteo");
            }
        }

        // Recent/forecast data (ECMWF IFS)
        if (string.Compare(endDate, cutoff, StringComparison.Ordinal) > 0)
        {
            var fcastStart = string.Compare(startDate, cutoff, StringComparison.Ordinal) > 0
                ? startDate
                : NextDay(cutoff);
            var url = $"https://api.open-meteo.com/v1/forecast?latitude={lat}&longitude={lon}" +
                      $"&daily=temperature_2m_max,temperature_2m_min" +
                      $"&temperature_unit=fahrenheit" +
                      $"&start_date={fcastStart}&end_date={endDate}" +
                      $"&timezone=America%2FNew_York" +
                      $"&models=ecmwf_ifs025";
            try
            {
                var json = await client.GetStringAsync(url, ct);
                var resp = JsonSerializer.Deserialize<OpenMeteoResponse>(json, JsonOptions);
                if (resp?.Daily is not null)
                {
                    allTimes.AddRange(resp.Daily.Time);
                    allMax.AddRange(resp.Daily.Temperature_2m_max);
                    allMin.AddRange(resp.Daily.Temperature_2m_min);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch forecast weather data from Open-Meteo");
            }
        }

        return CalculateGdd(allTimes, allMax, allMin, baseTempF, tempOffset);
    }

    private static IReadOnlyList<DailyGddEntry> CalculateGdd(
        List<string> times, List<double> maxTemps, List<double> minTemps,
        int baseTempF, int tempOffset)
    {
        const double MaxTempCap = 86.0;
        var results = new List<DailyGddEntry>();
        double cumulative = 0;

        for (var i = 0; i < times.Count; i++)
        {
            var tmax = Math.Min(maxTemps[i] + tempOffset, MaxTempCap);
            var tmin = minTemps[i];
            var daily = Math.Max(0, (tmax + tmin) / 2.0 - baseTempF);
            cumulative += daily;
            results.Add(new DailyGddEntry
            {
                Date = times[i],
                TempMax = maxTemps[i],
                TempMin = minTemps[i],
                Gdd = Math.Round(daily, 1),
                CumulativeGdd = Math.Round(cumulative, 1),
            });
        }

        return results.AsReadOnly();
    }

    private async Task<IReadOnlyList<DailyGddEntry>> CalculateGddFromEcowittAsync(
        string startDate, string endDate, int baseTempF, int tempOffset, CancellationToken ct)
    {
        var from = DateTime.Parse(startDate, System.Globalization.CultureInfo.InvariantCulture);
        var to = DateTime.Parse(endDate, System.Globalization.CultureInfo.InvariantCulture).AddDays(1);

        var readings = await _weatherService.GetHistoryAsync(from, to, limit: 50000, ct);

        // Group by date and compute daily high/low in Fahrenheit
        var byDate = new Dictionary<string, (double Max, double Min)>();
        foreach (var r in readings)
        {
            if (r.OutdoorTempC is null) continue;
            var dateKey = r.Timestamp.ToString("yyyy-MM-dd");
            var tempF = r.OutdoorTempC.Value * 9.0 / 5.0 + 32.0;

            if (byDate.TryGetValue(dateKey, out var existing))
            {
                byDate[dateKey] = (Math.Max(existing.Max, tempF), Math.Min(existing.Min, tempF));
            }
            else
            {
                byDate[dateKey] = (tempF, tempF);
            }
        }

        const double maxTempCap = 86.0;
        var times = byDate.Keys.OrderBy(d => d).ToList();
        var results = new List<DailyGddEntry>();
        double cumulative = 0;

        foreach (var date in times)
        {
            if (string.Compare(date, startDate, StringComparison.Ordinal) < 0) continue;
            var (max, min) = byDate[date];
            var tmax = Math.Min(max + tempOffset, maxTempCap);
            var daily = Math.Max(0, (tmax + min) / 2.0 - baseTempF);
            cumulative += daily;
            results.Add(new DailyGddEntry
            {
                Date = date,
                TempMax = max,
                TempMin = min,
                Gdd = Math.Round(daily, 1),
                CumulativeGdd = Math.Round(cumulative, 1),
            });
        }

        return results.AsReadOnly();
    }

    private async Task<(double Lat, double Lon)> GeocodeZipAsync(string zip, CancellationToken ct)
    {
        var client = _httpClientFactory.CreateClient();
        var url = $"https://api.zippopotam.us/us/{Uri.EscapeDataString(zip)}";
        var json = await client.GetStringAsync(url, ct);

        using var doc = JsonDocument.Parse(json);
        var places = doc.RootElement.GetProperty("places");
        var first = places[0];
        var lat = double.Parse(first.GetProperty("latitude").GetString()!, System.Globalization.CultureInfo.InvariantCulture);
        var lon = double.Parse(first.GetProperty("longitude").GetString()!, System.Globalization.CultureInfo.InvariantCulture);
        return (lat, lon);
    }

    private async Task CacheGddDataAsync(string uid, int year, IReadOnlyList<DailyGddEntry> entries, CancellationToken ct)
    {
        var cacheDoc = new GddCacheEntry
        {
            Year = year,
            Data = entries.Select(e => new GddCacheDay
            {
                Date = e.Date,
                TempMax = e.TempMax,
                TempMin = e.TempMin,
                Gdd = e.Gdd,
                CumulativeGdd = e.CumulativeGdd,
            }).ToList(),
            UpdatedAt = DateTime.UtcNow.ToString("o"),
        };

        await _db.Collection("users").Document(uid)
            .Collection("gddCache").Document(year.ToString())
            .SetAsync(cacheDoc, cancellationToken: ct);
    }

    private static string NextDay(string dateStr)
    {
        var d = DateTime.Parse(dateStr).AddDays(1);
        return d.ToString("yyyy-MM-dd");
    }
}
