using LawncareApi.Extensions;
using LawncareApi.Models;
using LawncareApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LawncareApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ZonesController : ControllerBase
{
    private readonly IZoneService _service;

    public ZonesController(IZoneService service) => _service = service;

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<YardZone>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var uid = User.GetFirebaseUid();
        return Ok(await _service.GetAllAsync(uid, ct));
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(YardZone), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(string id, CancellationToken ct)
    {
        var uid = User.GetFirebaseUid();
        var zone = await _service.GetByIdAsync(uid, id, ct);
        return zone is null ? NotFound() : Ok(zone);
    }

    [HttpPost]
    [ProducesResponseType(typeof(YardZone), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] YardZoneRequest request, CancellationToken ct)
    {
        var uid = User.GetFirebaseUid();
        var zone = await _service.CreateAsync(uid, request, ct);
        return CreatedAtAction(nameof(GetById), new { id = zone.Id }, zone);
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(YardZone), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(string id, [FromBody] YardZoneRequest request, CancellationToken ct)
    {
        var uid = User.GetFirebaseUid();
        var updated = await _service.UpdateAsync(uid, id, request, ct);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        var uid = User.GetFirebaseUid();
        return await _service.DeleteAsync(uid, id, ct) ? NoContent() : NotFound();
    }
}
