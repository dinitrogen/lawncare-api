using Google.Cloud.Firestore;

namespace LawncareApi.Models;

[FirestoreData]
public class Product
{
    [FirestoreDocumentId]
    public string? Id { get; set; }

    [FirestoreProperty]
    public string Name { get; set; } = string.Empty;

    [FirestoreProperty]
    public string Type { get; set; } = "other";

    [FirestoreProperty]
    public string? ActiveIngredient { get; set; }

    [FirestoreProperty]
    public double? ApplicationRatePerKSqFt { get; set; }

    [FirestoreProperty]
    public string? ApplicationRateUnit { get; set; }

    [FirestoreProperty]
    public double? GddWindowMin { get; set; }

    [FirestoreProperty]
    public double? GddWindowMax { get; set; }

    [FirestoreProperty]
    public int? ReapplyIntervalDays { get; set; }

    [FirestoreProperty]
    public string? Notes { get; set; }

    [FirestoreProperty]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class ProductRequest
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "other";
    public string? ActiveIngredient { get; set; }
    public double? ApplicationRatePerKSqFt { get; set; }
    public string? ApplicationRateUnit { get; set; }
    public double? GddWindowMin { get; set; }
    public double? GddWindowMax { get; set; }
    public int? ReapplyIntervalDays { get; set; }
    public string? Notes { get; set; }
}
