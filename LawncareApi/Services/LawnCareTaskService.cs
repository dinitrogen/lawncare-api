using Google.Cloud.Firestore;
using LawncareApi.Models;

namespace LawncareApi.Services;

/// <inheritdoc cref="ILawnCareTaskService"/>
public class LawnCareTaskService : ILawnCareTaskService
{
    private const string Collection = "lawn_care_tasks";
    private readonly FirestoreDb _db;

    public LawnCareTaskService(FirestoreDb db) => _db = db;

    public async Task<IReadOnlyList<LawnCareTask>> GetAllAsync(CancellationToken ct = default)
    {
        var snapshot = await _db.Collection(Collection)
            .OrderByDescending(nameof(LawnCareTask.CreatedAt))
            .GetSnapshotAsync(ct);

        return snapshot.Documents
            .Select(d => d.ConvertTo<LawnCareTask>())
            .ToList()
            .AsReadOnly();
    }

    public async Task<LawnCareTask?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var snapshot = await _db.Collection(Collection).Document(id).GetSnapshotAsync(ct);
        return snapshot.Exists ? snapshot.ConvertTo<LawnCareTask>() : null;
    }

    public async Task<LawnCareTask> CreateAsync(LawnCareTaskRequest request, CancellationToken ct = default)
    {
        var task = MapRequestToTask(request, new LawnCareTask());
        task.CreatedAt = DateTime.UtcNow;
        task.UpdatedAt = DateTime.UtcNow;

        var doc = _db.Collection(Collection).Document();
        task.Id = doc.Id;
        await doc.SetAsync(task, cancellationToken: ct);
        return task;
    }

    public async Task<LawnCareTask?> UpdateAsync(string id, LawnCareTaskRequest request, CancellationToken ct = default)
    {
        var docRef = _db.Collection(Collection).Document(id);
        var snapshot = await docRef.GetSnapshotAsync(ct);

        if (!snapshot.Exists) return null;

        var task = snapshot.ConvertTo<LawnCareTask>();
        MapRequestToTask(request, task);
        task.UpdatedAt = DateTime.UtcNow;

        if (request.IsCompleted && !task.CompletedAt.HasValue)
            task.CompletedAt = DateTime.UtcNow;
        else if (!request.IsCompleted)
            task.CompletedAt = null;

        await docRef.SetAsync(task, cancellationToken: ct);
        return task;
    }

    public async Task<bool> DeleteAsync(string id, CancellationToken ct = default)
    {
        var docRef = _db.Collection(Collection).Document(id);
        var snapshot = await docRef.GetSnapshotAsync(ct);

        if (!snapshot.Exists) return false;

        await docRef.DeleteAsync(cancellationToken: ct);
        return true;
    }

    private static LawnCareTask MapRequestToTask(LawnCareTaskRequest request, LawnCareTask task)
    {
        task.Title = request.Title;
        task.Description = request.Description;
        task.Category = request.Category;
        task.DueDate = request.DueDate;
        task.IsCompleted = request.IsCompleted;
        task.Notes = request.Notes;
        return task;
    }
}
