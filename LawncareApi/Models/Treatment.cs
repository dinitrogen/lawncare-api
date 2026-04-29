using Google.Cloud.Firestore;

namespace LawncareApi.Models;

[FirestoreData]
public class TreatmentLineItem
{
    [FirestoreProperty]
    public string ProductId { get; set; } = string.Empty;

    [FirestoreProperty]
    public string ProductName { get; set; } = string.Empty;

    [FirestoreProperty]
    public double AmountApplied { get; set; }

    [FirestoreProperty]
    public string AmountUnit { get; set; } = string.Empty;

    [FirestoreProperty]
    public string? ProductConcentration { get; set; }
}

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
    public DateTime ApplicationDate { get; set; }

    [FirestoreProperty]
    public string? ApplicationType { get; set; }

    [FirestoreProperty]
    public double? WaterVolume { get; set; }

    [FirestoreProperty]
    public double? SpreaderSetting { get; set; }

    [FirestoreProperty]
    public string? ApplicationRate { get; set; }

    [FirestoreProperty]
    public IList<TreatmentLineItem> LineItems { get; set; } = [];

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

    // Legacy single-product fields retained for backward compatibility
    [FirestoreProperty]
    public string? ProductId { get; set; }

    [FirestoreProperty]
    public string? ProductName { get; set; }

    [FirestoreProperty]
    public double? AmountApplied { get; set; }

    [FirestoreProperty]
    public string? AmountUnit { get; set; }

    [FirestoreProperty]
    public string? ProductConcentration { get; set; }
}

public class TreatmentLineItemRequest
{
    public string ProductId { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public double AmountApplied { get; set; }
    public string AmountUnit { get; set; } = string.Empty;
    public string? ProductConcentration { get; set; }
}

public class TreatmentRequest
{
    public IList<string> ZoneIds { get; set; } = [];
    public IList<string> ZoneNames { get; set; } = [];
    public DateTime ApplicationDate { get; set; }
    public string? ApplicationType { get; set; }
    public double? WaterVolume { get; set; }
    public double? SpreaderSetting { get; set; }
    public string? ApplicationRate { get; set; }
    public IList<TreatmentLineItemRequest> LineItems { get; set; } = [];
    public string? WeatherConditions { get; set; }
    public double? Temperature { get; set; }
    public string? Notes { get; set; }
    public double? Gdd { get; set; }
    public IList<string>? PhotoIds { get; set; }
}
