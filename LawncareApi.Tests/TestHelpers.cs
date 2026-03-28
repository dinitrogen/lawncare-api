using LawncareApi.Models;
using LawncareApi.Services;

namespace LawncareApi.Tests;

/// <summary>
/// In-memory implementation of <see cref="IWeatherService"/> for unit tests.
/// </summary>
internal sealed class InMemoryWeatherService : IWeatherService
{
    private readonly List<WeatherReading> _readings = [];

    public Task<WeatherReading> SaveReadingAsync(WeatherReading reading, CancellationToken ct = default)
    {
        reading.Id ??= Guid.NewGuid().ToString();
        _readings.Add(reading);
        return Task.FromResult(reading);
    }

    public Task<WeatherReading?> GetCurrentReadingAsync(CancellationToken ct = default)
    {
        var latest = _readings.OrderByDescending(r => r.Timestamp).FirstOrDefault();
        return Task.FromResult(latest);
    }

    public Task<IReadOnlyList<WeatherReading>> GetHistoryAsync(
        DateTime from, DateTime to, int limit = 100, CancellationToken ct = default)
    {
        IReadOnlyList<WeatherReading> result = _readings
            .Where(r => r.Timestamp >= from && r.Timestamp <= to)
            .OrderByDescending(r => r.Timestamp)
            .Take(limit)
            .ToList()
            .AsReadOnly();

        return Task.FromResult(result);
    }
}

/// <summary>
/// In-memory implementation of <see cref="ILawnCareTaskService"/> for unit tests.
/// </summary>
internal sealed class InMemoryLawnCareTaskService : ILawnCareTaskService
{
    private readonly Dictionary<string, LawnCareTask> _store = [];

    public Task<IReadOnlyList<LawnCareTask>> GetAllAsync(CancellationToken ct = default)
    {
        IReadOnlyList<LawnCareTask> result = _store.Values
            .OrderByDescending(t => t.CreatedAt)
            .ToList()
            .AsReadOnly();

        return Task.FromResult(result);
    }

    public Task<LawnCareTask?> GetByIdAsync(string id, CancellationToken ct = default) =>
        Task.FromResult(_store.TryGetValue(id, out var t) ? t : null);

    public Task<LawnCareTask> CreateAsync(LawnCareTaskRequest request, CancellationToken ct = default)
    {
        var task = new LawnCareTask
        {
            Id = Guid.NewGuid().ToString(),
            Title = request.Title,
            Description = request.Description,
            Category = request.Category,
            DueDate = request.DueDate,
            IsCompleted = request.IsCompleted,
            Notes = request.Notes,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _store[task.Id!] = task;
        return Task.FromResult(task);
    }

    public Task<LawnCareTask?> UpdateAsync(string id, LawnCareTaskRequest request, CancellationToken ct = default)
    {
        if (!_store.TryGetValue(id, out var task)) return Task.FromResult<LawnCareTask?>(null);

        task.Title = request.Title;
        task.Description = request.Description;
        task.Category = request.Category;
        task.DueDate = request.DueDate;
        task.IsCompleted = request.IsCompleted;
        task.Notes = request.Notes;
        task.UpdatedAt = DateTime.UtcNow;

        if (request.IsCompleted && !task.CompletedAt.HasValue)
            task.CompletedAt = DateTime.UtcNow;
        else if (!request.IsCompleted)
            task.CompletedAt = null;

        return Task.FromResult<LawnCareTask?>(task);
    }

    public async Task<bool> DeleteAsync(string id, CancellationToken ct = default) =>
        await Task.FromResult(_store.Remove(id));
}
