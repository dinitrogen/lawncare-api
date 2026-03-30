using Google.Cloud.Firestore;
using LawncareApi.Models;

namespace LawncareApi.Services;

public interface IUserService
{
    Task<AppUser?> GetAsync(string uid, CancellationToken ct = default);
    Task<AppUser> CreateAsync(string uid, AppUserCreateRequest request, CancellationToken ct = default);
    Task<AppUser?> UpdateAsync(string uid, AppUserUpdateRequest request, CancellationToken ct = default);
}

public class UserService : IUserService
{
    private readonly FirestoreDb _db;

    public UserService(FirestoreDb db) => _db = db;

    public async Task<AppUser?> GetAsync(string uid, CancellationToken ct = default)
    {
        var snapshot = await _db.Collection("users").Document(uid).GetSnapshotAsync(ct);
        return snapshot.Exists ? snapshot.ConvertTo<AppUser>() : null;
    }

    public async Task<AppUser> CreateAsync(string uid, AppUserCreateRequest request, CancellationToken ct = default)
    {
        var user = new AppUser
        {
            Email = request.Email,
            DisplayName = request.DisplayName,
            ZipCode = request.ZipCode,
            GddBase = request.GddBase,
            GddStartMonth = request.GddStartMonth,
            GddStartDay = request.GddStartDay,
            TempOffset = request.TempOffset,
            CreatedAt = DateTime.UtcNow,
        };

        var docRef = _db.Collection("users").Document(uid);
        user.Id = uid;
        await docRef.SetAsync(user, cancellationToken: ct);
        return user;
    }

    public async Task<AppUser?> UpdateAsync(string uid, AppUserUpdateRequest request, CancellationToken ct = default)
    {
        var docRef = _db.Collection("users").Document(uid);
        var snapshot = await docRef.GetSnapshotAsync(ct);
        if (!snapshot.Exists) return null;

        var user = snapshot.ConvertTo<AppUser>();

        if (request.DisplayName is not null) user.DisplayName = request.DisplayName;
        if (request.ZipCode is not null) user.ZipCode = request.ZipCode;
        if (request.Latitude.HasValue) user.Latitude = request.Latitude;
        if (request.Longitude.HasValue) user.Longitude = request.Longitude;
        if (request.GddBase.HasValue) user.GddBase = request.GddBase.Value;
        if (request.GddStartMonth.HasValue) user.GddStartMonth = request.GddStartMonth.Value;
        if (request.GddStartDay.HasValue) user.GddStartDay = request.GddStartDay.Value;
        if (request.TempOffset.HasValue) user.TempOffset = request.TempOffset.Value;
        if (request.GddSource is not null) user.GddSource = request.GddSource;
        if (request.DiscordWebhookUrl is not null) user.DiscordWebhookUrl = request.DiscordWebhookUrl;
        if (request.NotificationPrefs is not null) user.NotificationPrefs = request.NotificationPrefs;

        await docRef.SetAsync(user, cancellationToken: ct);
        return user;
    }
}
