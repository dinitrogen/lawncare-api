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
}
