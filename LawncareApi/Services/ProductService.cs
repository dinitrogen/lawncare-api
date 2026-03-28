using Google.Cloud.Firestore;
using LawncareApi.Models;

namespace LawncareApi.Services;

public interface IProductService
{
    Task<IReadOnlyList<Product>> GetAllAsync(string uid, CancellationToken ct = default);
    Task<Product?> GetByIdAsync(string uid, string id, CancellationToken ct = default);
    Task<Product> CreateAsync(string uid, ProductRequest request, CancellationToken ct = default);
    Task<Product?> UpdateAsync(string uid, string id, ProductRequest request, CancellationToken ct = default);
    Task<bool> DeleteAsync(string uid, string id, CancellationToken ct = default);
}

public class ProductService : IProductService
{
    private readonly FirestoreDb _db;

    public ProductService(FirestoreDb db) => _db = db;

    private CollectionReference ProductsCol(string uid) =>
        _db.Collection("users").Document(uid).Collection("products");

    public async Task<IReadOnlyList<Product>> GetAllAsync(string uid, CancellationToken ct = default)
    {
        var snapshot = await ProductsCol(uid).GetSnapshotAsync(ct);
        return snapshot.Documents
            .Select(d => d.ConvertTo<Product>())
            .ToList()
            .AsReadOnly();
    }

    public async Task<Product?> GetByIdAsync(string uid, string id, CancellationToken ct = default)
    {
        var snapshot = await ProductsCol(uid).Document(id).GetSnapshotAsync(ct);
        return snapshot.Exists ? snapshot.ConvertTo<Product>() : null;
    }

    public async Task<Product> CreateAsync(string uid, ProductRequest request, CancellationToken ct = default)
    {
        var product = new Product
        {
            Name = request.Name,
            Type = request.Type,
            ActiveIngredient = request.ActiveIngredient,
            ApplicationRatePerKSqFt = request.ApplicationRatePerKSqFt,
            ApplicationRateUnit = request.ApplicationRateUnit,
            GddWindowMin = request.GddWindowMin,
            GddWindowMax = request.GddWindowMax,
            ReapplyIntervalDays = request.ReapplyIntervalDays,
            Notes = request.Notes,
            CreatedAt = DateTime.UtcNow,
        };

        var doc = ProductsCol(uid).Document();
        product.Id = doc.Id;
        await doc.SetAsync(product, cancellationToken: ct);
        return product;
    }

    public async Task<Product?> UpdateAsync(string uid, string id, ProductRequest request, CancellationToken ct = default)
    {
        var docRef = ProductsCol(uid).Document(id);
        var snapshot = await docRef.GetSnapshotAsync(ct);
        if (!snapshot.Exists) return null;

        var product = snapshot.ConvertTo<Product>();
        product.Name = request.Name;
        product.Type = request.Type;
        product.ActiveIngredient = request.ActiveIngredient;
        product.ApplicationRatePerKSqFt = request.ApplicationRatePerKSqFt;
        product.ApplicationRateUnit = request.ApplicationRateUnit;
        product.GddWindowMin = request.GddWindowMin;
        product.GddWindowMax = request.GddWindowMax;
        product.ReapplyIntervalDays = request.ReapplyIntervalDays;
        product.Notes = request.Notes;

        await docRef.SetAsync(product, cancellationToken: ct);
        return product;
    }

    public async Task<bool> DeleteAsync(string uid, string id, CancellationToken ct = default)
    {
        var docRef = ProductsCol(uid).Document(id);
        var snapshot = await docRef.GetSnapshotAsync(ct);
        if (!snapshot.Exists) return false;

        await docRef.DeleteAsync(cancellationToken: ct);
        return true;
    }
}
