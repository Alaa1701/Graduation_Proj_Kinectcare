using System.Security.Claims;
using KinectCare.API.DTOs.Children;
using KinectCare.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KinectCare.API.Controllers;

[ApiController]
[Route("api/children")]
[Authorize]
public class ChildrenController : ControllerBase
{
    private readonly IChildService _childService;
    private readonly IDashboardService _dashboardService;

    public ChildrenController(IChildService childService,
        IDashboardService dashboardService)
    {
        _childService = childService;
        _dashboardService = dashboardService;
    }

    // GET api/children — Admin يرى الكل
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAll()
    {
        var result = await _childService.GetAllChildrenAsync();
        return Ok(result);
    }

    // GET api/children/my — الأخصائي يرى أطفاله فقط
    [HttpGet("my")]
    [Authorize(Roles = "Specialist")]
    public async Task<IActionResult> GetMyChildren()
    {
        var userId = int.Parse(
            User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _childService
            .GetChildrenBySpecialistAsync(userId);
        return Ok(result);
    }

    // GET api/children/my-dashboard — Specialist Dashboard
    [HttpGet("my-dashboard")]
    [Authorize(Roles = "Specialist")]
    public async Task<IActionResult> GetSpecialistDashboard()
    {
        var userId = int.Parse(
            User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _dashboardService
            .GetSpecialistStatsAsync(userId);
        return Ok(result);
    }

    // GET api/children/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _childService.GetChildByIdAsync(id);
        return result.Success ? Ok(result) : NotFound(result);
    }

    // POST api/children — Admin فقط
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create(
        [FromBody] CreateChildDto dto)
    {
        var adminId = int.Parse(
            User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _childService
            .CreateChildAsync(adminId, dto);
        return result.Success
            ? CreatedAtAction(nameof(GetById),
                new { id = result.Data!.Id }, result)
            : BadRequest(result);
    }

    // PUT api/children/{id} — Admin فقط
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(
        int id, [FromBody] UpdateChildDto dto)
    {
        var result = await _childService.UpdateChildAsync(id, dto);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    // DELETE api/children/{id} — Admin فقط
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _childService.DeleteChildAsync(id);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}