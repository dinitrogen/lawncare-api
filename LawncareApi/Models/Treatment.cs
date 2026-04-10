using Google.Cloud.Firestore;

namespace LawncareApi.Models;

[FirestoreData]
public class Treatment
{
    [FirestoreDocumentId]
    public string? Id { get; set; }

    [FirestoreProperty]
    public IList<string> ZoneIds { get; set; } = [];

    [FirestoreProperty]
    public IList<string> ZoneNames { get; set; } = [];

    [FirestoreProperty]
    public string ProductId { get; set; } = string.Empty;

    [FirestoreProperty]
    public string ProductName { get; set; } = string.Empty;

    [FirestoreProperty]
    public DateTime ApplicationDate { get; set; }

    [FirestoreProperty]
    public double AmountApplied { get; set; }

    [FirestoreProperty]
    public string AmountUnit { get; set; } = string.Empty;

    [FirestoreProperty]
    public double? WaterVolume { get; set; }

    [FirestoreProperty]
    public string? ApplicationType { get; set; }

    [FirestoreProperty]
    public string? ApplicationRate { get; set; }

    [FirestoreProperty]
    public double? SpreaderSetting { get; set; }

    [FirestoreProperty]
    public string? WeatherConditions { get; set; }

    [FirestoreProperty]
    public double? Temperature { get; set; }

    [FirestoreProperty]
    public string? Notes { get; set; }

    [FirestoreProperty]
    public double? Gdd { get; set; }

    [FirestoreProperty]
    public IList<string>? PhotoIds { get; set; }

    [FirestoreProperty]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class TreatmentRequest
{
    public IList<string> ZoneIds { get; set; } = [];
    public IList<string> ZoneNames { get; set; } = [];
    public string ProductId { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public DateTime ApplicationDate { get; set; }
    public double AmountApplied { get; set; }
    public string AmountUnit { get; set; } = string.Empty;
    public double? WaterVolume { get; set; }
    public string? ApplicationType { get; set; }
    public string? ApplicationRate { get; set; }
    public double? SpreaderSetting { get; set; }
    public string? WeatherConditions { get; set; }
    public double? Temperature { get; set; }
    public string? Notes { get; set; }
    public double? Gdd { get; set; }
    public IList<string>? PhotoIds { get; set; }
}
