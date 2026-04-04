using LawncareApi.Extensions;
using LawncareApi.Models;
using LawncareApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LawncareApi.Controllers;

/// <summary>
/// CRUD for calendar reminders. Discord notifications are scheduled by
/// <see cref="ReminderNotificationWorker"/> and sent on the reminder's date/time,
/// not at creation.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RemindersController : ControllerBase
{
    private readonly IReminderService _service;
    private readonly ILogger<RemindersController> _logger;

    public RemindersController(
        IReminderService service,
        ILogger<RemindersController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<Reminder>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var uid = User.GetFirebaseUid();
        return Ok(await _service.GetAllAsync(uid, ct));
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(Reminder), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(string id, CancellationToken ct)
    {
        var uid = User.GetFirebaseUid();
        var reminder = await _service.GetByIdAsync(uid, id, ct);
        return reminder is null ? NotFound() : Ok(reminder);
    }

    [HttpPost]
    [ProducesResponseType(typeof(Reminder), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] ReminderRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            return BadRequest("Title is required.");

        if (string.IsNullOrWhiteSpace(request.Date))
            return BadRequest("Date is required.");

        if (!DateOnly.TryParseExact(request.Date, "yyyy-MM-dd", out _))
            return BadRequest("Date must be a valid date in yyyy-MM-dd format.");

        var uid = User.GetFirebaseUid();
        var reminder = await _service.CreateAsync(uid, request, ct);

        return CreatedAtAction(nameof(GetById), new { id = reminder.Id }, reminder);
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(Reminder), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(string id, [FromBody] ReminderRequest request, CancellationToken ct)
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
