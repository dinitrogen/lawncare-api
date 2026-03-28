using LawncareApi.Extensions;
using LawncareApi.Models;
using LawncareApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LawncareApi.Controllers;

/// <summary>
/// GDD (Growing Degree Day) data endpoint. Fetches weather data from Open-Meteo,
/// calculates GDD using the user's profile settings, caches results, and returns them.
/// Replaces the client-side GDD calculation that previously ran in the Angular app.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class GddController : ControllerBase
{
    private readonly IGddApiService _gddService;

    public GddController(IGddApiService gddService) => _gddService = gddService;

    /// <summary>
    /// Fetches GDD data for the authenticated user based on their profile settings
    /// (zip code, base temp, start date, temp offset). Returns daily entries with
    /// cumulative GDD values.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<DailyGddEntry>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetGddData(CancellationToken ct)
    {
        var uid = User.GetFirebaseUid();
        var entries = await _gddService.GetGddDataAsync(uid, ct);
        return Ok(entries);
    }
}
