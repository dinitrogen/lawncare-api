using LawncareApi.Extensions;
using LawncareApi.Models;
using LawncareApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LawncareApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SeasonalController : ControllerBase
{
    private readonly ISeasonalService _service;

    public SeasonalController(ISeasonalService service) => _service = service;

    [HttpGet("{year:int}")]
    [ProducesResponseType(typeof(IList<SeasonalTaskStatus>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStatuses(int year, CancellationToken ct)
    {
        var uid = User.GetFirebaseUid();
        return Ok(await _service.GetStatusesAsync(uid, year, ct));
    }

    [HttpPut]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SaveStatuses([FromBody] SeasonalStatusRequest request, CancellationToken ct)
    {
        var uid = User.GetFirebaseUid();
        await _service.SaveStatusesAsync(uid, request, ct);
        return NoContent();
    }
}
