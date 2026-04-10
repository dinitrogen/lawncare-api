using Google.Cloud.Firestore;
using LawncareApi.Models;

namespace LawncareApi.Services;

public interface ITreatmentService
{
    Task<IReadOnlyList<Treatment>> GetAllAsync(string uid, CancellationToken ct = default);
    Task<Treatment?> GetByIdAsync(string uid, string id, CancellationToken ct = default);
    Task<Treatment> CreateAsync(string uid, TreatmentRequest request, CancellationToken ct = default);
    Task<Treatment?> UpdateAsync(string uid, string id, TreatmentRequest request, CancellationToken ct = default);
    Task<bool> DeleteAsync(string uid, string id, CancellationToken ct = default);
}

public class TreatmentService : ITreatmentService
{
    private readonly FirestoreDb _db;

    public TreatmentService(FirestoreDb db) => _db = db;

    private CollectionReference TreatmentsCol(string uid) =>
        _db.Collection("users").Document(uid).Collection("treatments");

    public async Task<IReadOnlyList<Treatment>> GetAllAsync(string uid, CancellationToken ct = default)
    {
        var snapshot = await TreatmentsCol(uid)
            .OrderByDescending(nameof(Treatment.ApplicationDate))
            .GetSnapshotAsync(ct);

        return snapshot.Documents
            .Select(d => d.ConvertTo<Treatment>())
            .ToList()
            .AsReadOnly();
    }

    public async Task<Treatment?> GetByIdAsync(string uid, string id, CancellationToken ct = default)
    {
        var snapshot = await TreatmentsCol(uid).Document(id).GetSnapshotAsync(ct);
        return snapshot.Exists ? snapshot.ConvertTo<Treatment>() : null;
    }

    public async Task<Treatment> CreateAsync(string uid, TreatmentRequest request, CancellationToken ct = default)
    {
        var treatment = new Treatment
        {
            ZoneIds = request.ZoneIds,
            ZoneNames = request.ZoneNames,
            ProductId = request.ProductId,
            ProductName = request.ProductName,
            ApplicationDate = request.ApplicationDate,
            AmountApplied = request.AmountApplied,
            AmountUnit = request.AmountUnit,
            WaterVolume = request.WaterVolume,
            ApplicationType = request.ApplicationType,
            ApplicationRate = request.ApplicationRate,
            SpreaderSetting = request.SpreaderSetting,
            WeatherConditions = request.WeatherConditions,
            Temperature = request.Temperature,
            Notes = request.Notes,
            Gdd = request.Gdd,
            PhotoIds = request.PhotoIds,
            CreatedAt = DateTime.UtcNow,
        };

        var doc = TreatmentsCol(uid).Document();
        treatment.Id = doc.Id;
        await doc.SetAsync(treatment, cancellationToken: ct);
        return treatment;
    }

    public async Task<Treatment?> UpdateAsync(string uid, string id, TreatmentRequest request, CancellationToken ct = default)
    {
        var docRef = TreatmentsCol(uid).Document(id);
        var snapshot = await docRef.GetSnapshotAsync(ct);
        if (!snapshot.Exists) return null;

        var treatment = snapshot.ConvertTo<Treatment>();
        treatment.ZoneIds = request.ZoneIds;
        treatment.ZoneNames = request.ZoneNames;
        treatment.ProductId = request.ProductId;
        treatment.ProductName = request.ProductName;
        treatment.ApplicationDate = request.ApplicationDate;
        treatment.AmountApplied = request.AmountApplied;
        treatment.AmountUnit = request.AmountUnit;
        treatment.WaterVolume = request.WaterVolume;
        treatment.ApplicationType = request.ApplicationType;
        treatment.ApplicationRate = request.ApplicationRate;
        treatment.SpreaderSetting = request.SpreaderSetting;
        treatment.WeatherConditions = request.WeatherConditions;
        treatment.Temperature = request.Temperature;
        treatment.Notes = request.Notes;
        treatment.Gdd = request.Gdd;
        treatment.PhotoIds = request.PhotoIds;

        await docRef.SetAsync(treatment, cancellationToken: ct);
        return treatment;
    }

    public async Task<bool> DeleteAsync(string uid, string id, CancellationToken ct = default)
    {
        var docRef = TreatmentsCol(uid).Document(id);
        var snapshot = await docRef.GetSnapshotAsync(ct);
        if (!snapshot.Exists) return false;

        await docRef.DeleteAsync(cancellationToken: ct);
        return true;
    }
}
