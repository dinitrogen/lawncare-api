using Google.Cloud.Firestore;
using LawncareApi.Models;

namespace LawncareApi.Services;

public interface ISoilTestService
{
    Task<IReadOnlyList<SoilTest>> GetAllAsync(string uid, CancellationToken ct = default);
    Task<SoilTest> CreateAsync(string uid, SoilTestRequest request, CancellationToken ct = default);
    Task<SoilTest?> UpdateAsync(string uid, string id, SoilTestRequest request, CancellationToken ct = default);
    Task<bool> DeleteAsync(string uid, string id, CancellationToken ct = default);
}

public class SoilTestService : ISoilTestService
{
    private readonly FirestoreDb _db;

    public SoilTestService(FirestoreDb db) => _db = db;

    private CollectionReference Col(string uid) =>
        _db.Collection("users").Document(uid).Collection("soilTests");

    public async Task<IReadOnlyList<SoilTest>> GetAllAsync(string uid, CancellationToken ct = default)
    {
        var snapshot = await Col(uid).GetSnapshotAsync(ct);
        return snapshot.Documents
            .Select(d => d.ConvertTo<SoilTest>())
            .ToList()
            .AsReadOnly();
    }

    public async Task<SoilTest> CreateAsync(string uid, SoilTestRequest request, CancellationToken ct = default)
    {
        var test = new SoilTest
        {
            ZoneId = request.ZoneId,
            ZoneName = request.ZoneName,
            TestDate = request.TestDate,
            Ph = request.Ph,
            Nitrogen = request.Nitrogen,
            Phosphorus = request.Phosphorus,
            Potassium = request.Potassium,
            OrganicMatter = request.OrganicMatter,
            Recommendations = request.Recommendations,
            LabName = request.LabName,
            PhotoIds = request.PhotoIds,
            Notes = request.Notes,
            CreatedAt = DateTime.UtcNow,
        };

        var doc = Col(uid).Document();
        test.Id = doc.Id;
        await doc.SetAsync(test, cancellationToken: ct);
        return test;
    }

    public async Task<SoilTest?> UpdateAsync(string uid, string id, SoilTestRequest request, CancellationToken ct = default)
    {
        var docRef = Col(uid).Document(id);
        var snapshot = await docRef.GetSnapshotAsync(ct);
        if (!snapshot.Exists) return null;

        var test = snapshot.ConvertTo<SoilTest>();
        test.ZoneId = request.ZoneId;
        test.ZoneName = request.ZoneName;
        test.TestDate = request.TestDate;
        test.Ph = request.Ph;
        test.Nitrogen = request.Nitrogen;
        test.Phosphorus = request.Phosphorus;
        test.Potassium = request.Potassium;
        test.OrganicMatter = request.OrganicMatter;
        test.Recommendations = request.Recommendations;
        test.LabName = request.LabName;
        test.PhotoIds = request.PhotoIds;
        test.Notes = request.Notes;

        await docRef.SetAsync(test, cancellationToken: ct);
        return test;
    }

    public async Task<bool> DeleteAsync(string uid, string id, CancellationToken ct = default)
    {
        var docRef = Col(uid).Document(id);
        var snapshot = await docRef.GetSnapshotAsync(ct);
        if (!snapshot.Exists) return false;

        await docRef.DeleteAsync(cancellationToken: ct);
        return true;
    }
}
