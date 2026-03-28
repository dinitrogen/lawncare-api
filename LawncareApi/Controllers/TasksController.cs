using LawncareApi.Models;
using LawncareApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LawncareApi.Controllers;

/// <summary>CRUD endpoints for lawn care tasks consumed by the Angular PWA.</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TasksController : ControllerBase
{
    private readonly ILawnCareTaskService _taskService;
    private readonly ILogger<TasksController> _logger;

    public TasksController(ILawnCareTaskService taskService, ILogger<TasksController> logger)
    {
        _taskService = taskService;
        _logger = logger;
    }

    /// <summary>Returns all lawn care tasks, newest first.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<LawnCareTask>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var tasks = await _taskService.GetAllAsync(ct);
        return Ok(tasks);
    }

    /// <summary>Returns a single task by its Firestore document ID.</summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(LawnCareTask), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(string id, CancellationToken ct)
    {
        var task = await _taskService.GetByIdAsync(id, ct);
        return task is null ? NotFound() : Ok(task);
    }

    /// <summary>Creates a new lawn care task.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(LawnCareTask), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] LawnCareTaskRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            return BadRequest("Title is required.");

        var created = await _taskService.CreateAsync(request, ct);
        _logger.LogInformation("Created lawn care task {Id}: {Title}", created.Id, created.Title);

        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>Updates an existing task.</summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(LawnCareTask), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(string id, [FromBody] LawnCareTaskRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            return BadRequest("Title is required.");

        var updated = await _taskService.UpdateAsync(id, request, ct);
        return updated is null ? NotFound() : Ok(updated);
    }

    /// <summary>Deletes a task by ID.</summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        var deleted = await _taskService.DeleteAsync(id, ct);
        return deleted ? NoContent() : NotFound();
    }
}
