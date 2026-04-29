using System.Security.Claims;
using KinectCare.API.DTOs.Sessions;
using KinectCare.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KinectCare.API.Controllers;

[ApiController]
[Route("api/sessions")]
[Authorize]
public class SessionsController : ControllerBase
{
    private readonly ISessionService _sessionService;

    public SessionsController(ISessionService sessionService)
    {
        _sessionService = sessionService;
    }

    // POST api/sessions
    [HttpPost]
    [Authorize(Roles = "Specialist")]
    [RequestSizeLimit(600_000_000)]
    public async Task<IActionResult> Create(
        [FromForm] CreateSessionDto dto,
        IFormFile video)
    {
        var specialistId = int.Parse(
            User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var result = await _sessionService
            .CreateSessionAsync(specialistId, dto, video);

        return result.Success
            ? CreatedAtAction(nameof(GetById),
                new { id = result.Data!.Id }, result)
            : BadRequest(result);
    }

    // GET api/sessions/child/{childId}
    [HttpGet("child/{childId}")]
    public async Task<IActionResult> GetByChild(int childId)
    {
        var result = await _sessionService
            .GetSessionsByChildAsync(childId);
        return Ok(result);
    }

    // GET api/sessions/my
    [HttpGet("my")]
    [Authorize(Roles = "Specialist")]
    public async Task<IActionResult> GetMySessions()
    {
        var specialistId = int.Parse(
            User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _sessionService
            .GetSessionsBySpecialistAsync(specialistId);
        return Ok(result);
    }

    // GET api/sessions/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _sessionService
            .GetSessionByIdAsync(id);
        return result.Success ? Ok(result) : NotFound(result);
    }

    // GET api/sessions/{id}/analysis
    [HttpGet("{id}/analysis")]
    public async Task<IActionResult> GetAnalysis(int id)
    {
        var result = await _sessionService.GetAnalysisAsync(id);
        return result.Success ? Ok(result) : NotFound(result);
    }

    // POST api/sessions/{id}/approve
    [HttpPost("{id}/approve")]
    [Authorize(Roles = "Specialist")]
    public async Task<IActionResult> Approve(int id)
    {
        var specialistId = int.Parse(
            User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _sessionService
            .ApproveAnalysisAsync(id, specialistId);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    // POST api/sessions/{id}/reanalyze
    [HttpPost("{id}/reanalyze")]
    [Authorize(Roles = "Specialist")]
    public async Task<IActionResult> ReAnalyze(int id)
    {
        var specialistId = int.Parse(
            User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var result = await _sessionService
            .ReAnalyzeSessionAsync(id, specialistId);

        return result.Success ? Ok(result) : BadRequest(result);
    }

    // DELETE api/sessions/{id}
    [HttpDelete("{id}")]
    [Authorize(Roles = "Specialist,Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _sessionService.DeleteSessionAsync(id);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}