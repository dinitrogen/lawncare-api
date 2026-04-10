using Google.Cloud.Firestore;
using LawncareApi.Models;

namespace LawncareApi.Services;

/// <inheritdoc cref="IWeatherService"/>
public class WeatherService : IWeatherService
{
    private const string Collection = "weather_readings";
    private readonly FirestoreDb _db;

    public WeatherService(FirestoreDb db) => _db = db;

    public async Task<WeatherReading> SaveReadingAsync(WeatherReading reading, CancellationToken ct = default)
    {
        var col = _db.Collection(Collection);
        var doc = string.IsNullOrEmpty(reading.Id)
            ? col.Document()
            : col.Document(reading.Id);

        reading.Id = doc.Id;
        await doc.SetAsync(reading, cancellationToken: ct);
        return reading;
    }

    public async Task<WeatherReading?> GetCurrentReadingAsync(CancellationToken ct = default)
    {
        var snapshot = await _db.Collection(Collection)
            .OrderByDescending(nameof(WeatherReading.Timestamp))
            .Limit(1)
            .GetSnapshotAsync(ct);

        return snapshot.Count == 0
            ? null
            : snapshot.Documents[0].ConvertTo<WeatherReading>();
    }

    public async Task<IReadOnlyList<WeatherReading>> GetHistoryAsync(
        DateTime from, DateTime to, int limit = 100, CancellationToken ct = default)
    {
        var snapshot = await _db.Collection(Collection)
            .WhereGreaterThanOrEqualTo(nameof(WeatherReading.Timestamp), from)
            .WhereLessThanOrEqualTo(nameof(WeatherReading.Timestamp), to)
            .OrderByDescending(nameof(WeatherReading.Timestamp))
            .Limit(limit)
            .GetSnapshotAsync(ct);

        return snapshot.Documents
            .Select(d => d.ConvertTo<WeatherReading>())
            .ToList()
            .AsReadOnly();
    }

    public async Task<IReadOnlyList<DailySummaryDto>> GetDailySummariesAsync(
        DateTime from, DateTime to, CancellationToken ct = default)
    {
        var snapshot = await _db.Collection(Collection)
            .WhereGreaterThanOrEqualTo(nameof(WeatherReading.Timestamp), from)
            .WhereLessThanOrEqualTo(nameof(WeatherReading.Timestamp), to)
            .OrderBy(nameof(WeatherReading.Timestamp))
            .GetSnapshotAsync(ct);

        var readings = snapshot.Documents
            .Select(d => d.ConvertTo<WeatherReading>())
            .ToList();

        return readings
            .GroupBy(r => r.Timestamp.ToString("yyyy-MM-dd"))
            .Select(g =>
            {
                var temps = g.Where(r => r.OutdoorTempC.HasValue).Select(r => r.OutdoorTempC!.Value).ToList();
                var humidities = g.Where(r => r.OutdoorHumidityPct.HasValue).Select(r => r.OutdoorHumidityPct!.Value).ToList();
                var soilMoistures = g.SelectMany(r => r.SoilMoisturePct ?? []).Select(v => (double)v).ToList();
                var soilTemps = g.SelectMany(r => r.SoilTempC ?? []).ToList();

                return new DailySummaryDto
                {
                    Date = g.Key,
                    HighTempC = temps.Count > 0 ? temps.Max() : 0,
                    LowTempC = temps.Count > 0 ? temps.Min() : 0,
                    AvgHumidityPct = humidities.Count > 0 ? humidities.Average() : 0,
                    AvgSoilMoisturePct = soilMoistures.Count > 0 ? soilMoistures.Average() : null,
                    AvgSoilTempC = soilTemps.Count > 0 ? soilTemps.Average() : null,
                };
            })
            .OrderBy(s => s.Date)
            .ToList()
            .AsReadOnly();
    }
}
