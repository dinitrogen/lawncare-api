using LawncareApi.Extensions;
using LawncareApi.Models;
using LawncareApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LawncareApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;

    public UserController(IUserService userService) => _userService = userService;

    [HttpGet]
    [ProducesResponseType(typeof(AppUser), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var uid = User.GetFirebaseUid();
        var user = await _userService.GetAsync(uid, ct);
        return user is null ? NotFound() : Ok(user);
    }

    [HttpPost]
    [ProducesResponseType(typeof(AppUser), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] AppUserCreateRequest request, CancellationToken ct)
    {
        var uid = User.GetFirebaseUid();
        var user = await _userService.CreateAsync(uid, request, ct);
        return CreatedAtAction(nameof(Get), user);
    }

    [HttpPut]
    [ProducesResponseType(typeof(AppUser), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update([FromBody] AppUserUpdateRequest request, CancellationToken ct)
    {
        var uid = User.GetFirebaseUid();
        var updated = await _userService.UpdateAsync(uid, request, ct);
        return updated is null ? NotFound() : Ok(updated);
    }
}
