using Google.Cloud.Firestore;

namespace LawncareApi.Models;

[FirestoreData]
public class SoilTest
{
    [FirestoreDocumentId]
    public string? Id { get; set; }

    [FirestoreProperty]
    public string ZoneId { get; set; } = string.Empty;

    [FirestoreProperty]
    public string ZoneName { get; set; } = string.Empty;

    [FirestoreProperty]
    public DateTime TestDate { get; set; }

    [FirestoreProperty]
    public double? Ph { get; set; }

    [FirestoreProperty]
    public double? Nitrogen { get; set; }

    [FirestoreProperty]
    public double? Phosphorus { get; set; }

    [FirestoreProperty]
    public double? Potassium { get; set; }

    [FirestoreProperty]
    public double? OrganicMatter { get; set; }

    [FirestoreProperty]
    public string? Recommendations { get; set; }

    [FirestoreProperty]
    public string? LabName { get; set; }

    [FirestoreProperty]
    public IList<string>? PhotoIds { get; set; }

    [FirestoreProperty]
    public string? Notes { get; set; }

    [FirestoreProperty]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class SoilTestRequest
{
    public string ZoneId { get; set; } = string.Empty;
    public string ZoneName { get; set; } = string.Empty;
    public DateTime TestDate { get; set; }
    public double? Ph { get; set; }
    public double? Nitrogen { get; set; }
    public double? Phosphorus { get; set; }
    public double? Potassium { get; set; }
    public double? OrganicMatter { get; set; }
    public string? Recommendations { get; set; }
    public string? LabName { get; set; }
    public IList<string>? PhotoIds { get; set; }
    public string? Notes { get; set; }
}
