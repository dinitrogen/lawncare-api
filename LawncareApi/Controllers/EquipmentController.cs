using LawncareApi.Extensions;
using LawncareApi.Models;
using LawncareApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LawncareApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EquipmentController : ControllerBase
{
    private readonly IEquipmentService _service;

    public EquipmentController(IEquipmentService service) => _service = service;

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<Equipment>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var uid = User.GetFirebaseUid();
        return Ok(await _service.GetAllAsync(uid, ct));
    }

    [HttpPost]
    [ProducesResponseType(typeof(Equipment), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] EquipmentRequest request, CancellationToken ct)
    {
        var uid = User.GetFirebaseUid();
        var equip = await _service.CreateAsync(uid, request, ct);
        return Created($"/api/equipment/{equip.Id}", equip);
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(Equipment), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(string id, [FromBody] EquipmentRequest request, CancellationToken ct)
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

    // Maintenance logs
    [HttpGet("logs")]
    [ProducesResponseType(typeof(IReadOnlyList<MaintenanceLog>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLogs(CancellationToken ct)
    {
        var uid = User.GetFirebaseUid();
        return Ok(await _service.GetLogsAsync(uid, ct));
    }

    [HttpPost("logs")]
    [ProducesResponseType(typeof(MaintenanceLog), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateLog([FromBody] MaintenanceLogRequest request, CancellationToken ct)
    {
        var uid = User.GetFirebaseUid();
        var log = await _service.CreateLogAsync(uid, request, ct);
        return Created($"/api/equipment/logs/{log.Id}", log);
    }

    [HttpDelete("logs/{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteLog(string id, CancellationToken ct)
    {
        var uid = User.GetFirebaseUid();
        return await _service.DeleteLogAsync(uid, id, ct) ? NoContent() : NotFound();
    }
}
