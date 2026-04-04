using LawncareApi.Extensions;
using LawncareApi.Models;
using LawncareApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LawncareApi.Controllers;

/// <summary>
/// CRUD for calendar reminders. Sends a Discord notification on creation
/// if <see cref="ReminderRequest.SendDiscordReminder"/> is true and the user
/// has a webhook configured.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RemindersController : ControllerBase
{
    private readonly IReminderService _service;
    private readonly IUserService _userService;
    private readonly DiscordNotificationService _discord;
    private readonly ILogger<RemindersController> _logger;

    public RemindersController(
        IReminderService service,
        IUserService userService,
        DiscordNotificationService discord,
        ILogger<RemindersController> logger)
    {
        _service = service;
        _userService = userService;
        _discord = discord;
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

        // Send Discord notification (best-effort, non-blocking)
        if (reminder.SendDiscordReminder)
        {
            try
            {
                var user = await _userService.GetAsync(uid);
                if (user?.DiscordWebhookUrl is not null)
                {
                    await _discord.SendReminderNotificationAsync(
                        user.DiscordWebhookUrl,
                        reminder.Title,
                        reminder.Date,
                        reminder.Time);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Discord notification failed for reminder {Id}", reminder.Id);
            }
        }

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
