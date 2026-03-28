using Google.Cloud.Firestore;
using LawncareApi.Models;

namespace LawncareApi.Services;

public interface IEquipmentService
{
    Task<IReadOnlyList<Equipment>> GetAllAsync(string uid, CancellationToken ct = default);
    Task<Equipment> CreateAsync(string uid, EquipmentRequest request, CancellationToken ct = default);
    Task<Equipment?> UpdateAsync(string uid, string id, EquipmentRequest request, CancellationToken ct = default);
    Task<bool> DeleteAsync(string uid, string id, CancellationToken ct = default);

    Task<IReadOnlyList<MaintenanceLog>> GetLogsAsync(string uid, CancellationToken ct = default);
    Task<MaintenanceLog> CreateLogAsync(string uid, MaintenanceLogRequest request, CancellationToken ct = default);
    Task<bool> DeleteLogAsync(string uid, string id, CancellationToken ct = default);
}

public class EquipmentService : IEquipmentService
{
    private readonly FirestoreDb _db;

    public EquipmentService(FirestoreDb db) => _db = db;

    private CollectionReference EquipCol(string uid) =>
        _db.Collection("users").Document(uid).Collection("equipment");

    private CollectionReference LogsCol(string uid) =>
        _db.Collection("users").Document(uid).Collection("maintenanceLogs");

    public async Task<IReadOnlyList<Equipment>> GetAllAsync(string uid, CancellationToken ct = default)
    {
        var snapshot = await EquipCol(uid).GetSnapshotAsync(ct);
        return snapshot.Documents
            .Select(d => d.ConvertTo<Equipment>())
            .ToList()
            .AsReadOnly();
    }

    public async Task<Equipment> CreateAsync(string uid, EquipmentRequest request, CancellationToken ct = default)
    {
        var equip = new Equipment
        {
            Name = request.Name,
            Type = request.Type,
            Brand = request.Brand,
            Model = request.Model,
            PurchaseDate = request.PurchaseDate,
            Notes = request.Notes,
            CreatedAt = DateTime.UtcNow,
        };

        var doc = EquipCol(uid).Document();
        equip.Id = doc.Id;
        await doc.SetAsync(equip, cancellationToken: ct);
        return equip;
    }

    public async Task<Equipment?> UpdateAsync(string uid, string id, EquipmentRequest request, CancellationToken ct = default)
    {
        var docRef = EquipCol(uid).Document(id);
        var snapshot = await docRef.GetSnapshotAsync(ct);
        if (!snapshot.Exists) return null;

        var equip = snapshot.ConvertTo<Equipment>();
        equip.Name = request.Name;
        equip.Type = request.Type;
        equip.Brand = request.Brand;
        equip.Model = request.Model;
        equip.PurchaseDate = request.PurchaseDate;
        equip.Notes = request.Notes;

        await docRef.SetAsync(equip, cancellationToken: ct);
        return equip;
    }

    public async Task<bool> DeleteAsync(string uid, string id, CancellationToken ct = default)
    {
        var docRef = EquipCol(uid).Document(id);
        var snapshot = await docRef.GetSnapshotAsync(ct);
        if (!snapshot.Exists) return false;

        await docRef.DeleteAsync(cancellationToken: ct);
        return true;
    }

    public async Task<IReadOnlyList<MaintenanceLog>> GetLogsAsync(string uid, CancellationToken ct = default)
    {
        var snapshot = await LogsCol(uid).GetSnapshotAsync(ct);
        return snapshot.Documents
            .Select(d => d.ConvertTo<MaintenanceLog>())
            .ToList()
            .AsReadOnly();
    }

    public async Task<MaintenanceLog> CreateLogAsync(string uid, MaintenanceLogRequest request, CancellationToken ct = default)
    {
        var log = new MaintenanceLog
        {
            EquipmentId = request.EquipmentId,
            EquipmentName = request.EquipmentName,
            Date = request.Date,
            Type = request.Type,
            Description = request.Description,
            Cost = request.Cost,
            Notes = request.Notes,
            CreatedAt = DateTime.UtcNow,
        };

        var doc = LogsCol(uid).Document();
        log.Id = doc.Id;
        await doc.SetAsync(log, cancellationToken: ct);
        return log;
    }

    public async Task<bool> DeleteLogAsync(string uid, string id, CancellationToken ct = default)
    {
        var docRef = LogsCol(uid).Document(id);
        var snapshot = await docRef.GetSnapshotAsync(ct);
        if (!snapshot.Exists) return false;

        await docRef.DeleteAsync(cancellationToken: ct);
        return true;
    }
}
