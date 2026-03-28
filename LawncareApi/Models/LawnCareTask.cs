using Google.Cloud.Firestore;

namespace LawncareApi.Models;

/// <summary>Category of a lawn care task.</summary>
public enum TaskCategory
{
    Mowing,
    Fertilizing,
    Watering,
    Weeding,
    Seeding,
    Aerating,
    Edging,
    Mulching,
    PestControl,
    Pruning,
    Other
}

/// <summary>
/// A lawn care task managed by the Angular PWA and persisted in Firestore.
/// </summary>
[FirestoreData]
public class LawnCareTask
{
    [FirestoreDocumentId]
    public string? Id { get; set; }

    [FirestoreProperty]
    public string Title { get; set; } = string.Empty;

    [FirestoreProperty]
    public string? Description { get; set; }

    [FirestoreProperty]
    public TaskCategory Category { get; set; } = TaskCategory.Other;

    [FirestoreProperty]
    public DateTime? DueDate { get; set; }

    [FirestoreProperty]
    public bool IsCompleted { get; set; }

    [FirestoreProperty]
    public DateTime? CompletedAt { get; set; }

    [FirestoreProperty]
    public string? Notes { get; set; }

    [FirestoreProperty]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [FirestoreProperty]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>Payload accepted when creating or updating a task.</summary>
public class LawnCareTaskRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TaskCategory Category { get; set; } = TaskCategory.Other;
    public DateTime? DueDate { get; set; }
    public bool IsCompleted { get; set; }
    public string? Notes { get; set; }
}
