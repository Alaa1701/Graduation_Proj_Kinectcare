using System.Security.Claims;
using KinectCare.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KinectCare.API.Controllers;

[ApiController]
[Route("api/parent")]
[Authorize(Roles = "Parent")]
public class ParentController : ControllerBase
{
    private readonly IChildService _childService;
    private readonly IReportService _reportService;
    private readonly IRehabPlanService _planService;
    private readonly ISessionService _sessionService;

    public ParentController(
        IChildService childService,
        IReportService reportService,
        IRehabPlanService planService,
        ISessionService sessionService)
    {
        _childService = childService;
        _reportService = reportService;
        _planService = planService;
        _sessionService = sessionService;
    }

    // GET api/parent/children — أطفاله فقط
    [HttpGet("children")]
    public async Task<IActionResult> GetMyChildren()
    {
        var parentId = int.Parse(
            User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _childService
            .GetChildrenByParentAsync(parentId);
        return Ok(result);
    }

    // GET api/parent/children/{childId}/reports
    [HttpGet("children/{childId}/reports")]
    public async Task<IActionResult> GetChildReports(
        int childId)
    {
        // ولي الأمر يرى المنشورة فقط
        var result = await _reportService
            .GetReportsByChildAsync(childId, visibleOnly: true);
        return Ok(result);
    }

    // GET api/parent/children/{childId}/plan
    [HttpGet("children/{childId}/plan")]
    public async Task<IActionResult> GetChildPlan(int childId)
    {
        var result = await _planService
            .GetLatestPlanByChildAsync(childId);
        return result.Success ? Ok(result) : NotFound(result);
    }

    // GET api/parent/children/{childId}/sessions
    [HttpGet("children/{childId}/sessions")]
    public async Task<IActionResult> GetChildSessions(
        int childId)
    {
        var result = await _sessionService
            .GetSessionsByChildAsync(childId);
        return Ok(result);
    }
}