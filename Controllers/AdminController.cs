using System.Security.Claims;
using KinectCare.API.DTOs.Users;
using KinectCare.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KinectCare.API.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IDashboardService _dashboardService;

    public AdminController(IUserService userService,
        IDashboardService dashboardService)
    {
        _userService = userService;
        _dashboardService = dashboardService;
    }

    // ── Dashboard ─────────────────────────────────────────
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard()
    {
        var result = await _dashboardService.GetAdminStatsAsync();
        return Ok(result);
    }

    // ── Specialists ───────────────────────────────────────
    [HttpGet("specialists")]
    public async Task<IActionResult> GetSpecialists()
    {
        var result = await _userService.GetAllSpecialistsAsync();
        return Ok(result);
    }

    [HttpGet("specialists/{id}")]
    public async Task<IActionResult> GetSpecialist(int id)
    {
        var result = await _userService.GetSpecialistByIdAsync(id);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpPost("specialists")]
    public async Task<IActionResult> CreateSpecialist(
        [FromBody] CreateSpecialistDto dto)
    {
        var result = await _userService.CreateSpecialistAsync(dto);
        return result.Success
            ? CreatedAtAction(nameof(GetSpecialist),
                new { id = result.Data!.Id }, result)
            : BadRequest(result);
    }

    [HttpPut("specialists/{id}")]
    public async Task<IActionResult> UpdateSpecialist(
        int id, [FromBody] UpdateUserDto dto)
    {
        var result = await _userService.UpdateUserAsync(id, dto);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    // ── Parents ───────────────────────────────────────────
    [HttpGet("parents")]
    public async Task<IActionResult> GetParents()
    {
        var result = await _userService.GetAllParentsAsync();
        return Ok(result);
    }

    [HttpGet("parents/{id}")]
    public async Task<IActionResult> GetParent(int id)
    {
        var result = await _userService.GetParentByIdAsync(id);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpPost("parents")]
    public async Task<IActionResult> CreateParent(
        [FromBody] CreateParentDto dto)
    {
        var result = await _userService.CreateParentAsync(dto);
        return result.Success
            ? CreatedAtAction(nameof(GetParent),
                new { id = result.Data!.Id }, result)
            : BadRequest(result);
    }

    [HttpPut("parents/{id}")]
    public async Task<IActionResult> UpdateParent(
        int id, [FromBody] UpdateUserDto dto)
    {
        var result = await _userService.UpdateUserAsync(id, dto);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPut("parents/{id}/permissions")]
    public async Task<IActionResult> UpdatePermissions(
        int id, [FromBody] UpdatePermissionsDto dto)
    {
        var result = await _userService
            .UpdatePermissionsAsync(id, dto);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    // ── Shared ────────────────────────────────────────────
    [HttpPut("users/{id}/toggle-status")]
    public async Task<IActionResult> ToggleStatus(int id)
    {
        var result = await _userService.ToggleUserStatusAsync(id);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpDelete("users/{id}")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var result = await _userService.DeleteUserAsync(id);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}