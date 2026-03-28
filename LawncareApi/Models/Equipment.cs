using Google.Cloud.Firestore;

namespace LawncareApi.Models;

[FirestoreData]
public class Equipment
{
    [FirestoreDocumentId]
    public string? Id { get; set; }

    [FirestoreProperty]
    public string Name { get; set; } = string.Empty;

    [FirestoreProperty]
    public string Type { get; set; } = "other";

    [FirestoreProperty]
    public string? Brand { get; set; }

    [FirestoreProperty]
    public string? Model { get; set; }

    [FirestoreProperty]
    public DateTime? PurchaseDate { get; set; }

    [FirestoreProperty]
    public string? Notes { get; set; }

    [FirestoreProperty]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class EquipmentRequest
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "other";
    public string? Brand { get; set; }
    public string? Model { get; set; }
    public DateTime? PurchaseDate { get; set; }
    public string? Notes { get; set; }
}

[FirestoreData]
public class MaintenanceLog
{
    [FirestoreDocumentId]
    public string? Id { get; set; }

    [FirestoreProperty]
    public string EquipmentId { get; set; } = string.Empty;

    [FirestoreProperty]
    public string EquipmentName { get; set; } = string.Empty;

    [FirestoreProperty]
    public DateTime Date { get; set; }

    [FirestoreProperty]
    public string Type { get; set; } = "other";

    [FirestoreProperty]
    public string Description { get; set; } = string.Empty;

    [FirestoreProperty]
    public double? Cost { get; set; }

    [FirestoreProperty]
    public string? Notes { get; set; }

    [FirestoreProperty]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class MaintenanceLogRequest
{
    public string EquipmentId { get; set; } = string.Empty;
    public string EquipmentName { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string Type { get; set; } = "other";
    public string Description { get; set; } = string.Empty;
    public double? Cost { get; set; }
    public string? Notes { get; set; }
}
