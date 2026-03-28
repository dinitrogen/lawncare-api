using Google.Cloud.Firestore;
using LawncareApi.Models;

namespace LawncareApi.Services;

public interface IZoneService
{
    Task<IReadOnlyList<YardZone>> GetAllAsync(string uid, CancellationToken ct = default);
    Task<YardZone?> GetByIdAsync(string uid, string id, CancellationToken ct = default);
    Task<YardZone> CreateAsync(string uid, YardZoneRequest request, CancellationToken ct = default);
    Task<YardZone?> UpdateAsync(string uid, string id, YardZoneRequest request, CancellationToken ct = default);
    Task<bool> DeleteAsync(string uid, string id, CancellationToken ct = default);
}

public class ZoneService : IZoneService
{
    private readonly FirestoreDb _db;

    public ZoneService(FirestoreDb db) => _db = db;

    private CollectionReference ZonesCol(string uid) =>
        _db.Collection("users").Document(uid).Collection("zones");

    public async Task<IReadOnlyList<YardZone>> GetAllAsync(string uid, CancellationToken ct = default)
    {
        var snapshot = await ZonesCol(uid).GetSnapshotAsync(ct);
        return snapshot.Documents
            .Select(d => d.ConvertTo<YardZone>())
            .ToList()
            .AsReadOnly();
    }

    public async Task<YardZone?> GetByIdAsync(string uid, string id, CancellationToken ct = default)
    {
        var snapshot = await ZonesCol(uid).Document(id).GetSnapshotAsync(ct);
        return snapshot.Exists ? snapshot.ConvertTo<YardZone>() : null;
    }

    public async Task<YardZone> CreateAsync(string uid, YardZoneRequest request, CancellationToken ct = default)
    {
        var zone = new YardZone
        {
            Name = request.Name,
            Area = request.Area,
            GrassType = request.GrassType,
            SoilType = request.SoilType,
            SunExposure = request.SunExposure,
            SketchData = request.SketchData,
            Notes = request.Notes,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        var doc = ZonesCol(uid).Document();
        zone.Id = doc.Id;
        await doc.SetAsync(zone, cancellationToken: ct);
        return zone;
    }

    public async Task<YardZone?> UpdateAsync(string uid, string id, YardZoneRequest request, CancellationToken ct = default)
    {
        var docRef = ZonesCol(uid).Document(id);
        var snapshot = await docRef.GetSnapshotAsync(ct);
        if (!snapshot.Exists) return null;

        var zone = snapshot.ConvertTo<YardZone>();
        zone.Name = request.Name;
        zone.Area = request.Area;
        zone.GrassType = request.GrassType;
        zone.SoilType = request.SoilType;
        zone.SunExposure = request.SunExposure;
        zone.SketchData = request.SketchData;
        zone.Notes = request.Notes;
        zone.UpdatedAt = DateTime.UtcNow;

        await docRef.SetAsync(zone, cancellationToken: ct);
        return zone;
    }

    public async Task<bool> DeleteAsync(string uid, string id, CancellationToken ct = default)
    {
        var docRef = ZonesCol(uid).Document(id);
        var snapshot = await docRef.GetSnapshotAsync(ct);
        if (!snapshot.Exists) return false;

        await docRef.DeleteAsync(cancellationToken: ct);
        return true;
    }
}
