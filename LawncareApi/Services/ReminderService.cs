using Google.Cloud.Firestore;
using LawncareApi.Models;

namespace LawncareApi.Services;

public interface IReminderService
{
    Task<IReadOnlyList<Reminder>> GetAllAsync(string uid, CancellationToken ct = default);
    Task<Reminder?> GetByIdAsync(string uid, string id, CancellationToken ct = default);
    Task<Reminder> CreateAsync(string uid, ReminderRequest request, CancellationToken ct = default);
    Task<Reminder?> UpdateAsync(string uid, string id, ReminderRequest request, CancellationToken ct = default);
    Task<bool> DeleteAsync(string uid, string id, CancellationToken ct = default);
}

public class ReminderService : IReminderService
{
    private readonly FirestoreDb _db;

    public ReminderService(FirestoreDb db) => _db = db;

    private CollectionReference RemindersCol(string uid) =>
        _db.Collection("users").Document(uid).Collection("reminders");

    public async Task<IReadOnlyList<Reminder>> GetAllAsync(string uid, CancellationToken ct = default)
    {
        var snapshot = await RemindersCol(uid)
            .OrderByDescending(nameof(Reminder.Date))
            .GetSnapshotAsync(ct);

        return snapshot.Documents
            .Select(d => d.ConvertTo<Reminder>())
            .ToList()
            .AsReadOnly();
    }

    public async Task<Reminder?> GetByIdAsync(string uid, string id, CancellationToken ct = default)
    {
        var snapshot = await RemindersCol(uid).Document(id).GetSnapshotAsync(ct);
        return snapshot.Exists ? snapshot.ConvertTo<Reminder>() : null;
    }

    public async Task<Reminder> CreateAsync(string uid, ReminderRequest request, CancellationToken ct = default)
    {
        var reminder = new Reminder
        {
            Title = request.Title,
            Date = request.Date,
            Time = request.Time,
            Notes = request.Notes,
            SendDiscordReminder = request.SendDiscordReminder,
            CreatedAt = DateTime.UtcNow,
        };

        var doc = RemindersCol(uid).Document();
        reminder.Id = doc.Id;
        await doc.SetAsync(reminder, cancellationToken: ct);
        return reminder;
    }

    public async Task<Reminder?> UpdateAsync(string uid, string id, ReminderRequest request, CancellationToken ct = default)
    {
        var docRef = RemindersCol(uid).Document(id);
        var snapshot = await docRef.GetSnapshotAsync(ct);
        if (!snapshot.Exists) return null;

        var reminder = snapshot.ConvertTo<Reminder>();
        reminder.Title = request.Title;
        reminder.Date = request.Date;
        reminder.Time = request.Time;
        reminder.Notes = request.Notes;
        reminder.SendDiscordReminder = request.SendDiscordReminder;
        // Reset so the scheduler re-evaluates delivery on the new date/time.
        reminder.NotificationSent = false;

        await docRef.SetAsync(reminder, cancellationToken: ct);
        return reminder;
    }

    public async Task<bool> DeleteAsync(string uid, string id, CancellationToken ct = default)
    {
        var docRef = RemindersCol(uid).Document(id);
        var snapshot = await docRef.GetSnapshotAsync(ct);
        if (!snapshot.Exists) return false;

        await docRef.DeleteAsync(cancellationToken: ct);
        return true;
    }
}
