using Google.Cloud.Firestore;

namespace LawncareApi.Models;

[FirestoreData]
public class SeasonalTaskStatus
{
    [FirestoreProperty]
    public string TaskId { get; set; } = string.Empty;

    [FirestoreProperty]
    public int Year { get; set; }

    [FirestoreProperty]
    public DateTime? CompletedAt { get; set; }

    [FirestoreProperty]
    public string? TreatmentId { get; set; }

    [FirestoreProperty]
    public bool Skipped { get; set; }

    [FirestoreProperty]
    public string? Notes { get; set; }
}

[FirestoreData]
public class SeasonalStatusDoc
{
    [FirestoreDocumentId]
    public string? Id { get; set; }

    [FirestoreProperty]
    public int Year { get; set; }

    [FirestoreProperty]
    public IList<SeasonalTaskStatus> Statuses { get; set; } = [];

    [FirestoreProperty]
    public string UpdatedAt { get; set; } = string.Empty;
}

public class SeasonalStatusRequest
{
    public int Year { get; set; }
    public IList<SeasonalTaskStatus> Statuses { get; set; } = [];
}
