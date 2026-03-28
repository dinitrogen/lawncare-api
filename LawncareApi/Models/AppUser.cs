using Google.Cloud.Firestore;

namespace LawncareApi.Models;

[FirestoreData]
public class NotificationPrefs
{
    [FirestoreProperty]
    public bool TreatmentReminders { get; set; } = true;

    [FirestoreProperty]
    public bool GddAlerts { get; set; } = true;

    [FirestoreProperty]
    public bool WeeklyDigest { get; set; } = true;
}

[FirestoreData]
public class AppUser
{
    [FirestoreDocumentId]
    public string? Id { get; set; }

    [FirestoreProperty]
    public string Email { get; set; } = string.Empty;

    [FirestoreProperty]
    public string DisplayName { get; set; } = string.Empty;

    [FirestoreProperty]
    public string ZipCode { get; set; } = string.Empty;

    [FirestoreProperty]
    public double? Latitude { get; set; }

    [FirestoreProperty]
    public double? Longitude { get; set; }

    [FirestoreProperty]
    public int GddBase { get; set; } = 50;

    [FirestoreProperty]
    public int GddStartMonth { get; set; } = 2;

    [FirestoreProperty]
    public int GddStartDay { get; set; } = 15;

    [FirestoreProperty]
    public int TempOffset { get; set; }

    [FirestoreProperty]
    public string? DiscordWebhookUrl { get; set; }

    [FirestoreProperty]
    public NotificationPrefs NotificationPrefs { get; set; } = new();

    [FirestoreProperty]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class AppUserCreateRequest
{
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;
    public int GddBase { get; set; } = 50;
    public int GddStartMonth { get; set; } = 2;
    public int GddStartDay { get; set; } = 15;
    public int TempOffset { get; set; }
}

public class AppUserUpdateRequest
{
    public string? DisplayName { get; set; }
    public string? ZipCode { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public int? GddBase { get; set; }
    public int? GddStartMonth { get; set; }
    public int? GddStartDay { get; set; }
    public int? TempOffset { get; set; }
    public string? DiscordWebhookUrl { get; set; }
    public NotificationPrefs? NotificationPrefs { get; set; }
}
