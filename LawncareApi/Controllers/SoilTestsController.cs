using LawncareApi.Extensions;
using LawncareApi.Models;
using LawncareApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LawncareApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SoilTestsController : ControllerBase
{
    private readonly ISoilTestService _service;

    public SoilTestsController(ISoilTestService service) => _service = service;

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<SoilTest>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var uid = User.GetFirebaseUid();
        return Ok(await _service.GetAllAsync(uid, ct));
    }

    [HttpPost]
    [ProducesResponseType(typeof(SoilTest), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] SoilTestRequest request, CancellationToken ct)
    {
        var uid = User.GetFirebaseUid();
        var test = await _service.CreateAsync(uid, request, ct);
        return Created($"/api/soiltests/{test.Id}", test);
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(SoilTest), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(string id, [FromBody] SoilTestRequest request, CancellationToken ct)
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
