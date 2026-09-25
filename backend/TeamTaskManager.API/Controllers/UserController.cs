using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TeamTaskManager.Business.Services;
using TeamTaskManager.DTO.User;
using TeamTaskManager.Entities;

namespace TeamTaskManager.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly UserManager<ApplicationUser> _userManager;

    public UserController(IUserService userService, UserManager<ApplicationUser> userManager)
    {
        _userService = userService;
        _userManager = userManager;
    }

    private async Task<bool> IsAdminAsync()
    {
        if (User.IsInRole("Admin"))
            return true;

        var userId = User.FindFirstValue("nameid")
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
            return false;

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
            return false;

        return await _userManager.IsInRoleAsync(user, "Admin");
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        if (!await IsAdminAsync())
            return Forbid();

        var users = await _userService.GetAllAsync();
        return Ok(users);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        if (!await IsAdminAsync())
            return Forbid();

        var user = await _userService.GetByIdAsync(id);
        return user is null ? NotFound(new { message = "Kullanıcı bulunamadı." }) : Ok(user);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] UserCreateRequest request)
    {
        if (!await IsAdminAsync())
            return Forbid();

        try
        {
            var createdUser = await _userService.CreateAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = createdUser.Id }, createdUser);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UserUpdateRequest request)
    {
        if (!await IsAdminAsync())
            return Forbid();

        try
        {
            var user = await _userService.UpdateAsync(id, request);
            return user is null ? NotFound(new { message = "Kullanıcı bulunamadı." }) : Ok(user);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UserStatusUpdateRequest request)
    {
        if (!await IsAdminAsync())
            return Forbid();

        try
        {
            var user = await _userService.UpdateStatusAsync(id, request.IsActive);
            return user is null ? NotFound(new { message = "Kullanıcı bulunamadı." }) : Ok(user);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
