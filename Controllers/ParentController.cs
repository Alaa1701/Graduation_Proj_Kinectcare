
#region old code
//using System.Security.Claims;
//using KinectCare.API.Services.Interfaces;
//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Mvc;

//namespace KinectCare.API.Controllers;

//[ApiController]
//[Route("api/parent")]
//[Authorize(Roles = "Parent")]
//public class ParentController : ControllerBase
//{
//    private readonly IChildService _childService;
//    private readonly IReportService _reportService;
//    private readonly IRehabPlanService _planService;
//    private readonly ISessionService _sessionService;

//    public ParentController(
//        IChildService childService,
//        IReportService reportService,
//        IRehabPlanService planService,
//        ISessionService sessionService)
//    {
//        _childService = childService;
//        _reportService = reportService;
//        _planService = planService;
//        _sessionService = sessionService;
//    }

//    // GET api/parent/children — أطفاله فقط
//    [HttpGet("children")]
//    public async Task<IActionResult> GetMyChildren()
//    {
//        var parentId = int.Parse(
//            User.FindFirstValue(ClaimTypes.NameIdentifier)!);
//        var result = await _childService
//            .GetChildrenByParentAsync(parentId);
//        return Ok(result);
//    }

//    // GET api/parent/children/{childId}/reports
//    [HttpGet("children/{childId}/reports")]
//    public async Task<IActionResult> GetChildReports(
//        int childId)
//    {
//        // ولي الأمر يرى المنشورة فقط
//        var result = await _reportService
//            .GetReportsByChildAsync(childId, visibleOnly: true);
//        return Ok(result);
//    }

//    // GET api/parent/children/{childId}/plan
//    [HttpGet("children/{childId}/plan")]
//    public async Task<IActionResult> GetChildPlan(int childId)
//    {
//        var result = await _planService
//            .GetLatestPlanByChildAsync(childId);
//        return result.Success ? Ok(result) : NotFound(result);
//    }

//    // GET api/parent/children/{childId}/sessions
//    [HttpGet("children/{childId}/sessions")]
//    public async Task<IActionResult> GetChildSessions(
//        int childId)
//    {
//        var result = await _sessionService
//            .GetSessionsByChildAsync(childId);
//        return Ok(result);
//    }
//}
#endregion

using System.Security.Claims;
using KinectCare.API.Data;
using KinectCare.API.DTOs.Common;
using KinectCare.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
    private readonly AppDbContext _db;

    public ParentController(
        IChildService childService,
        IReportService reportService,
        IRehabPlanService planService,
        ISessionService sessionService,
        AppDbContext db)
    {
        _childService = childService;
        _reportService = reportService;
        _planService = planService;
        _sessionService = sessionService;
        _db = db;
    }

    // ── Helper: استخراج parentId من التوكن ─────────────────
    private int GetParentId() =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // ── Helper: التحقق أن الطفل ينتمي لولي الأمر الحالي ────
    private async Task<bool> ChildBelongsToParent(int childId)
    {
        var parentId = GetParentId();
        return await _db.Children.AnyAsync(c =>
            c.Id == childId && c.ParentId == parentId);
    }

    // ─────────────────────────────────────────────────────────
    // GET api/parent/children — قائمة أطفاله فقط
    // ─────────────────────────────────────────────────────────
    [HttpGet("children")]
    public async Task<IActionResult> GetMyChildren()
    {
        var parentId = GetParentId();
        var result = await _childService
            .GetChildrenByParentAsync(parentId);
        return Ok(result);
    }

    // ─────────────────────────────────────────────────────────
    // GET api/parent/children/{childId}/overview
    // يُرجع كل بيانات الطفل دفعة واحدة لتجنب requests متعددة
    // ─────────────────────────────────────────────────────────
    [HttpGet("children/{childId}/overview")]
    public async Task<IActionResult> GetChildOverview(int childId)
    {
        // ✅ فحص أمني: الطفل ملكه فعلاً؟
        if (!await ChildBelongsToParent(childId))
            return Forbid();

        var child = await _db.Children
            .Include(c => c.Specialist)
            .FirstOrDefaultAsync(c => c.Id == childId);

        if (child == null)
            return NotFound(ApiResponse<string>.Fail("الطفل غير موجود"));

        // آخر جلسة
        var lastSession = await _db.Sessions
            .Where(s => s.ChildId == childId)
            .OrderByDescending(s => s.SessionDate)
            .FirstOrDefaultAsync();

        // عدد الجلسات المكتملة
        var completedCount = await _db.Sessions
            .CountAsync(s => s.ChildId == childId && s.Status == "Done");

        // الخطة الفعالة
        var activePlan = await _db.RehabilitationPlans
            .Where(p => p.ChildId == childId && p.IsActive)
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefaultAsync();

        // آخر 3 تقارير منشورة
        var latestReports = await _db.Reports
            .Include(r => r.Specialist)
            .Include(r => r.Session)
            .Where(r => r.ChildId == childId && r.IsVisibleToParent)
            .OrderByDescending(r => r.CreatedAt)
            .Take(3)
            .Select(r => new
            {
                r.Id,
                r.SessionId,
                r.CreatedAt,
                r.ProgressAssessment,
                r.SessionObservations,
                r.RecommendationsForParents,
                r.ReportFilePath,
                SpecialistName = r.Specialist.FullName,
                ExerciseType = r.Session != null ? r.Session.ExerciseType : "",
                SessionDate = r.Session != null ? r.Session.SessionDate : (DateTime?)null,
            })
            .ToListAsync();

        // متوسط التقدم من آخر 10 تحليلات
        var avgProgress = await _db.AIAnalysisResults
            .Where(a => a.Session.ChildId == childId && a.IsApprovedBySpecialist)
            .OrderByDescending(a => a.AnalyzedAt)
            .Take(10)
            .AverageAsync(a =>
                (double?)(a.PoseScore + a.HandScore + a.ActivityLevel + a.AttentionScore) / 4);

        var overview = new
        {
            // بيانات الطفل
            child.Id,
            child.FullName,
            child.DateOfBirth,
            Age = DateTime.Today.Year - child.DateOfBirth.Year,
            child.Gender,
            child.DiagnosisType,
            child.DiagnosisDetails,
            child.ProfileImagePath,
            child.Status,
            child.CreatedAt,

            // الأخصائي
            SpecialistId = child.SpecialistId,
            SpecialistName = child.Specialist?.FullName ?? "",

            // إحصاءات
            CompletedSessions = completedCount,
            AvgProgress = avgProgress.HasValue
                ? Math.Round(avgProgress.Value, 1) : 0.0,
            LastSessionDate = lastSession?.SessionDate,
            LastSessionType = lastSession?.ExerciseType ?? "",

            // الخطة الفعالة
            HasActivePlan = activePlan != null,
            ActivePlanTitle = activePlan?.Title ?? "",
            ActivePlanId = activePlan?.Id,

            // آخر التقارير
            LatestReports = latestReports,
        };

        return Ok(ApiResponse<object>.Ok(overview));
    }

    // ─────────────────────────────────────────────────────────
    // GET api/parent/children/{childId}/reports
    // ─────────────────────────────────────────────────────────
    [HttpGet("children/{childId}/reports")]
    public async Task<IActionResult> GetChildReports(int childId)
    {
        if (!await ChildBelongsToParent(childId))
            return Forbid();

        var result = await _reportService
            .GetReportsByChildAsync(childId, visibleOnly: true);
        return Ok(result);
    }

    // ─────────────────────────────────────────────────────────
    // GET api/parent/children/{childId}/plan
    // ─────────────────────────────────────────────────────────
    [HttpGet("children/{childId}/plan")]
    public async Task<IActionResult> GetChildPlan(int childId)
    {
        if (!await ChildBelongsToParent(childId))
            return Forbid();

        var result = await _planService
            .GetLatestPlanByChildAsync(childId);
        return result.Success ? Ok(result) : NotFound(result);
    }

    // ─────────────────────────────────────────────────────────
    // GET api/parent/children/{childId}/sessions
    // ─────────────────────────────────────────────────────────
    [HttpGet("children/{childId}/sessions")]
    public async Task<IActionResult> GetChildSessions(int childId)
    {
        if (!await ChildBelongsToParent(childId))
            return Forbid();

        var result = await _sessionService
            .GetSessionsByChildAsync(childId);
        return Ok(result);
    }

    // ─────────────────────────────────────────────────────────
    // GET api/parent/children/{childId}/progress
    // بيانات التقدم عبر الوقت من نتائج AI المعتمدة
    // ─────────────────────────────────────────────────────────
    [HttpGet("children/{childId}/progress")]
    public async Task<IActionResult> GetChildProgress(int childId)
    {
        if (!await ChildBelongsToParent(childId))
            return Forbid();

        var analysisData = await _db.AIAnalysisResults
            .Include(a => a.Session)
            .Where(a =>
                a.Session.ChildId == childId &&
                a.IsApprovedBySpecialist)
            .OrderBy(a => a.AnalyzedAt)
            .Take(20)
            .Select(a => new
            {
                Date = a.AnalyzedAt,
                Month = a.AnalyzedAt.ToString("MMM yyyy"),
                PoseScore = Math.Round(a.PoseScore, 1),
                HandScore = Math.Round(a.HandScore, 1),
                ActivityLevel = Math.Round(a.ActivityLevel, 1),
                AttentionScore = Math.Round(a.AttentionScore, 1),
                Overall = Math.Round(
                    (a.PoseScore + a.HandScore +
                     a.ActivityLevel + a.AttentionScore) / 4.0, 1),
                ExerciseType = a.Session.ExerciseType,
            })
            .ToListAsync();

        return Ok(ApiResponse<object>.Ok(analysisData));
    }
}