using System.Security.Claims;

namespace LawncareApi.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static string GetFirebaseUid(this ClaimsPrincipal principal) =>
        principal.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new UnauthorizedAccessException("Firebase UID not found in token claims.");
}
