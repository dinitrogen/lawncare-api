using LawncareApi.Models;

namespace LawncareApi.Services;

/// <summary>Abstracts weather data persistence and retrieval.</summary>
public interface IWeatherService
{
    /// <summary>Persist a normalised <see cref="WeatherReading"/> and return the stored entity.</summary>
    Task<WeatherReading> SaveReadingAsync(WeatherReading reading, CancellationToken ct = default);

    /// <summary>Return the most recently stored <see cref="WeatherReading"/>, or null if none exist.</summary>
    Task<WeatherReading?> GetCurrentReadingAsync(CancellationToken ct = default);

    /// <summary>Return readings stored between <paramref name="from"/> and <paramref name="to"/> (UTC), newest first.</summary>
    Task<IReadOnlyList<WeatherReading>> GetHistoryAsync(
        DateTime from, DateTime to, int limit = 100, CancellationToken ct = default);

    /// <summary>Return daily-aggregated summaries for the given date range.</summary>
    Task<IReadOnlyList<DailySummaryDto>> GetDailySummariesAsync(
        DateTime from, DateTime to, CancellationToken ct = default);
}
