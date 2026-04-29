using System.Security.Claims;
using KinectCare.API.DTOs.RehabPlans;
using KinectCare.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using KinectCare.API.Data;
using KinectCare.API.DTOs.Common;

namespace KinectCare.API.Controllers;

[ApiController]
[Route("api/rehab-plans")]
[Authorize]
public class RehabPlansController : ControllerBase
{
    private readonly IRehabPlanService _planService;
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;

    public RehabPlansController(
     IRehabPlanService planService,
     AppDbContext db,
     IWebHostEnvironment env)
    {
        _planService = planService;
        _db = db;
        _env = env;
    }

    // POST api/rehab-plans
    [HttpPost]
    [Authorize(Roles = "Specialist")]
    public async Task<IActionResult> Create(
        [FromBody] CreateRehabPlanDto dto)
    {
        var specialistId = int.Parse(
            User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _planService
            .CreatePlanAsync(specialistId, dto);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    // GET api/rehab-plans/child/{childId}/all — الأخصائي
    [HttpGet("child/{childId}/all")]
    [Authorize(Roles = "Specialist,Admin")]
    public async Task<IActionResult> GetAll(int childId)
    {
        var result = await _planService
            .GetAllPlansByChildAsync(childId);
        return Ok(result);
    }

    // GET api/rehab-plans/child/{childId}/latest — الأب
    [HttpGet("child/{childId}/latest")]
    public async Task<IActionResult> GetLatest(int childId)
    {
        var result = await _planService
            .GetLatestPlanByChildAsync(childId);
        return result.Success ? Ok(result) : NotFound(result);
    }

    // DELETE api/rehab-plans/{id}
    [HttpDelete("{id}")]
    [Authorize(Roles = "Specialist,Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _planService.DeletePlanAsync(id);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    // POST api/rehab-plans/{id}/upload-pdf
    [HttpPost("{id}/upload-pdf")]
    [Authorize(Roles = "Specialist")]
    public async Task<IActionResult> UploadPdf(
        int id, IFormFile file)
    {
        var plan = await _db.RehabilitationPlans
            .FindAsync(id);

        if (plan == null)
            return NotFound(ApiResponse<string>.Fail(
                "الخطة غير موجودة"));

        if (file == null || file.Length == 0)
            return BadRequest(ApiResponse<string>.Fail(
                "يرجى رفع ملف PDF"));

        if (!file.ContentType.Contains("pdf"))
            return BadRequest(ApiResponse<string>.Fail(
                "يجب أن يكون الملف بصيغة PDF"));

        if (file.Length > 10 * 1024 * 1024)
            return BadRequest(ApiResponse<string>.Fail(
                "حجم الملف أكبر من 10 MB"));

        var folder = Path.Combine(_env.WebRootPath, "plans");
        if (!Directory.Exists(folder))
            Directory.CreateDirectory(folder);

        var fileName =
            $"plan_{id}_{DateTime.Now:yyyyMMddHHmm}.pdf";
        var filePath = Path.Combine(folder, fileName);

        using var stream = new FileStream(
            filePath, FileMode.Create);
        await file.CopyToAsync(stream);

        plan.PlanFilePath = $"/plans/{fileName}";
        await _db.SaveChangesAsync();

        return Ok(ApiResponse<string>.Ok(
            plan.PlanFilePath,
            "تم رفع ملف الخطة بنجاح"));
    }

    // GET api/rehab-plans/{id}/download
    [HttpGet("{id}/download")]
    [Authorize]
    public async Task<IActionResult> DownloadPlan(int id)
    {
        var plan = await _db.RehabilitationPlans
            .FindAsync(id);

        if (plan?.PlanFilePath == null)
            return NotFound("الملف غير موجود");

        var fullPath = Path.Combine(
            _env.WebRootPath,
            plan.PlanFilePath.TrimStart('/'));

        if (!System.IO.File.Exists(fullPath))
            return NotFound("الملف غير موجود على السيرفر");

        var bytes = await System.IO.File
            .ReadAllBytesAsync(fullPath);

        return File(bytes, "application/pdf",
            $"plan_{id}.pdf");
    }

    // GET api/rehab-plans/{id}/view
    [HttpGet("{id}/view")]
    [Authorize]
    public async Task<IActionResult> ViewPlan(int id)
    {
        var plan = await _db.RehabilitationPlans.FindAsync(id);

        if (plan?.PlanFilePath == null)
            return NotFound("الملف غير موجود");

        var fullPath = Path.Combine(
            _env.WebRootPath,
            plan.PlanFilePath.TrimStart('/'));

        if (!System.IO.File.Exists(fullPath))
            return NotFound("الملف غير موجود على السيرفر");

        var bytes = await System.IO.File
            .ReadAllBytesAsync(fullPath);

        // inline = يفتح في المتصفح بدلاً من التحميل
        Response.Headers.Append(
            "Content-Disposition",
            $"inline; filename=\"plan_{id}.pdf\"");

        return File(bytes, "application/pdf");
    }

}