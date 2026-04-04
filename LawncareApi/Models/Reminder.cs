using Google.Cloud.Firestore;

namespace LawncareApi.Models;

[FirestoreData]
public class Reminder
{
    [FirestoreDocumentId]
    public string? Id { get; set; }

    [FirestoreProperty]
    public string Title { get; set; } = string.Empty;

    [FirestoreProperty]
    public string Date { get; set; } = string.Empty;

    [FirestoreProperty]
    public string? Time { get; set; }

    [FirestoreProperty]
    public string? Notes { get; set; }

    [FirestoreProperty]
    public bool SendDiscordReminder { get; set; }

    /// <summary>
    /// Set to <c>true</c> once the Discord notification for this reminder has been dispatched.
    /// The <see cref="ReminderNotificationWorker"/> uses this flag to avoid re-sending.
    /// </summary>
    [FirestoreProperty]
    public bool NotificationSent { get; set; }

    [FirestoreProperty]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class ReminderRequest
{
    public string Title { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public string? Time { get; set; }
    public string? Notes { get; set; }
    public bool SendDiscordReminder { get; set; }
}
