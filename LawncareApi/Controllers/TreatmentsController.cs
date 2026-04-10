using LawncareApi.Extensions;
using LawncareApi.Models;
using LawncareApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LawncareApi.Controllers;

/// <summary>
/// CRUD for lawn care treatments.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TreatmentsController : ControllerBase
{
    private readonly ITreatmentService _service;
    private readonly ILogger<TreatmentsController> _logger;

    public TreatmentsController(
        ITreatmentService service,
        ILogger<TreatmentsController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<Treatment>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var uid = User.GetFirebaseUid();
        return Ok(await _service.GetAllAsync(uid, ct));
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(Treatment), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(string id, CancellationToken ct)
    {
        var uid = User.GetFirebaseUid();
        var treatment = await _service.GetByIdAsync(uid, id, ct);
        return treatment is null ? NotFound() : Ok(treatment);
    }

    [HttpPost]
    [ProducesResponseType(typeof(Treatment), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] TreatmentRequest request, CancellationToken ct)
    {
        var uid = User.GetFirebaseUid();
        var treatment = await _service.CreateAsync(uid, request, ct);

        return CreatedAtAction(nameof(GetById), new { id = treatment.Id }, treatment);
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(Treatment), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(string id, [FromBody] TreatmentRequest request, CancellationToken ct)
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
