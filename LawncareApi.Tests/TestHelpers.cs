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

    public Task<IReadOnlyList<DailySummaryDto>> GetDailySummariesAsync(
        DateTime from, DateTime to, CancellationToken ct = default)
    {
        IReadOnlyList<DailySummaryDto> result = _readings
            .Where(r => r.Timestamp >= from && r.Timestamp <= to)
            .GroupBy(r => r.Timestamp.ToString("yyyy-MM-dd"))
            .Select(g =>
            {
                var temps = g.Where(r => r.OutdoorTempC.HasValue).Select(r => r.OutdoorTempC!.Value).ToList();
                return new DailySummaryDto
                {
                    Date = g.Key,
                    HighTempC = temps.Count > 0 ? temps.Max() : 0,
                    LowTempC = temps.Count > 0 ? temps.Min() : 0,
                    AvgHumidityPct = 0,
                };
            })
            .OrderBy(s => s.Date)
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

/// <summary>
/// In-memory implementation of <see cref="IReminderService"/> for unit tests.
/// </summary>
internal sealed class InMemoryReminderService : IReminderService
{
    private readonly Dictionary<string, Reminder> _store = [];

    public Task<IReadOnlyList<Reminder>> GetAllAsync(string uid, CancellationToken ct = default)
    {
        IReadOnlyList<Reminder> result = _store.Values
            .OrderByDescending(r => r.Date)
            .ToList()
            .AsReadOnly();

        return Task.FromResult(result);
    }

    public Task<Reminder?> GetByIdAsync(string uid, string id, CancellationToken ct = default) =>
        Task.FromResult(_store.TryGetValue(id, out var r) ? r : null);

    public Task<Reminder> CreateAsync(string uid, ReminderRequest request, CancellationToken ct = default)
    {
        var reminder = new Reminder
        {
            Id = Guid.NewGuid().ToString(),
            Title = request.Title,
            Date = request.Date,
            Time = request.Time,
            Notes = request.Notes,
            SendDiscordReminder = request.SendDiscordReminder,
            NotificationSent = false,
            CreatedAt = DateTime.UtcNow,
        };
        _store[reminder.Id!] = reminder;
        return Task.FromResult(reminder);
    }

    public Task<Reminder?> UpdateAsync(string uid, string id, ReminderRequest request, CancellationToken ct = default)
    {
        if (!_store.TryGetValue(id, out var reminder)) return Task.FromResult<Reminder?>(null);

        reminder.Title = request.Title;
        reminder.Date = request.Date;
        reminder.Time = request.Time;
        reminder.Notes = request.Notes;
        reminder.SendDiscordReminder = request.SendDiscordReminder;
        // Reset so the scheduler re-evaluates delivery on the new date/time.
        reminder.NotificationSent = false;

        return Task.FromResult<Reminder?>(reminder);
    }

    public async Task<bool> DeleteAsync(string uid, string id, CancellationToken ct = default) =>
        await Task.FromResult(_store.Remove(id));

    /// <summary>
    /// Test helper: simulates the background worker having already dispatched
    /// the notification for <paramref name="id"/>.
    /// </summary>
    public void MarkNotificationSentForTest(string id)
    {
        if (_store.TryGetValue(id, out var reminder))
            reminder.NotificationSent = true;
    }
}

/// <summary>
/// Stub <see cref="IUserService"/> that always returns null (no user profile) for unit tests.
/// </summary>
internal sealed class StubUserService : IUserService
{
    public Task<AppUser?> GetAsync(string uid, CancellationToken ct = default) =>
        Task.FromResult<AppUser?>(null);

    public Task<AppUser> CreateAsync(string uid, AppUserCreateRequest request, CancellationToken ct = default) =>
        throw new NotImplementedException();

    public Task<AppUser?> UpdateAsync(string uid, AppUserUpdateRequest request, CancellationToken ct = default) =>
        throw new NotImplementedException();
}

/// <summary>
/// Stub <see cref="IHttpClientFactory"/> that returns a default <see cref="HttpClient"/> for unit tests.
/// </summary>
internal sealed class StubHttpClientFactory : IHttpClientFactory
{
    public HttpClient CreateClient(string name) => new();
}

/// <summary>
/// In-memory implementation of <see cref="ITreatmentService"/> for unit tests.
/// </summary>
internal sealed class InMemoryTreatmentService : ITreatmentService
{
    private readonly Dictionary<string, Treatment> _store = [];

    public Task<IReadOnlyList<Treatment>> GetAllAsync(string uid, CancellationToken ct = default)
    {
        IReadOnlyList<Treatment> result = _store.Values
            .OrderByDescending(t => t.ApplicationDate)
            .ToList()
            .AsReadOnly();

        return Task.FromResult(result);
    }

    public Task<Treatment?> GetByIdAsync(string uid, string id, CancellationToken ct = default) =>
        Task.FromResult(_store.TryGetValue(id, out var t) ? t : null);

    public Task<Treatment> CreateAsync(string uid, TreatmentRequest request, CancellationToken ct = default)
    {
        var treatment = new Treatment
        {
            Id = Guid.NewGuid().ToString(),
            ZoneIds = request.ZoneIds,
            ZoneNames = request.ZoneNames,
            ProductId = request.ProductId,
            ProductName = request.ProductName,
            ApplicationDate = request.ApplicationDate,
            AmountApplied = request.AmountApplied,
            AmountUnit = request.AmountUnit,
            WaterVolume = request.WaterVolume,
            WeatherConditions = request.WeatherConditions,
            Temperature = request.Temperature,
            Notes = request.Notes,
            Gdd = request.Gdd,
            PhotoIds = request.PhotoIds,
            CreatedAt = DateTime.UtcNow,
        };
        _store[treatment.Id!] = treatment;
        return Task.FromResult(treatment);
    }

    public Task<Treatment?> UpdateAsync(string uid, string id, TreatmentRequest request, CancellationToken ct = default)
    {
        if (!_store.TryGetValue(id, out var treatment)) return Task.FromResult<Treatment?>(null);

        treatment.ZoneIds = request.ZoneIds;
        treatment.ZoneNames = request.ZoneNames;
        treatment.ProductId = request.ProductId;
        treatment.ProductName = request.ProductName;
        treatment.ApplicationDate = request.ApplicationDate;
        treatment.AmountApplied = request.AmountApplied;
        treatment.AmountUnit = request.AmountUnit;
        treatment.WaterVolume = request.WaterVolume;
        treatment.WeatherConditions = request.WeatherConditions;
        treatment.Temperature = request.Temperature;
        treatment.Notes = request.Notes;
        treatment.Gdd = request.Gdd;
        treatment.PhotoIds = request.PhotoIds;

        return Task.FromResult<Treatment?>(treatment);
    }

    public async Task<bool> DeleteAsync(string uid, string id, CancellationToken ct = default) =>
        await Task.FromResult(_store.Remove(id));
}
