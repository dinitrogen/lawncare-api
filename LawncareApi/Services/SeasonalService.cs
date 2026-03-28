using Google.Cloud.Firestore;
using LawncareApi.Models;

namespace LawncareApi.Services;

public interface ISeasonalService
{
    Task<IList<SeasonalTaskStatus>> GetStatusesAsync(string uid, int year, CancellationToken ct = default);
    Task SaveStatusesAsync(string uid, SeasonalStatusRequest request, CancellationToken ct = default);
}

public class SeasonalService : ISeasonalService
{
    private readonly FirestoreDb _db;

    public SeasonalService(FirestoreDb db) => _db = db;

    private DocumentReference StatusDoc(string uid, int year) =>
        _db.Collection("users").Document(uid).Collection("seasonalStatus").Document(year.ToString());

    public async Task<IList<SeasonalTaskStatus>> GetStatusesAsync(string uid, int year, CancellationToken ct = default)
    {
        var snapshot = await StatusDoc(uid, year).GetSnapshotAsync(ct);
        if (!snapshot.Exists) return [];

        var doc = snapshot.ConvertTo<SeasonalStatusDoc>();
        return doc.Statuses;
    }

    public async Task SaveStatusesAsync(string uid, SeasonalStatusRequest request, CancellationToken ct = default)
    {
        var doc = new SeasonalStatusDoc
        {
            Year = request.Year,
            Statuses = request.Statuses,
            UpdatedAt = DateTime.UtcNow.ToString("o"),
        };

        await StatusDoc(uid, request.Year).SetAsync(doc, cancellationToken: ct);
    }
}
