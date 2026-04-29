using System.Security.Claims;
using KinectCare.API.DTOs.Reports;
using KinectCare.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using KinectCare.API.Services;  // ← أضف هذا

namespace KinectCare.API.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;
    private readonly PdfService _pdfService;
    private readonly IWebHostEnvironment _env;


    public ReportsController(
     IReportService reportService,
     PdfService pdfService,
     IWebHostEnvironment env)
    {
        _reportService = reportService;
        _pdfService = pdfService;
        _env = env;
    }


    // POST api/reports — الأخصائي ينشئ تقرير
    [HttpPost]
    [Authorize(Roles = "Specialist")]
    public async Task<IActionResult> Create(
        [FromBody] CreateReportDto dto)
    {
        var specialistId = int.Parse(
            User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _reportService
            .CreateReportAsync(specialistId, dto);
        return result.Success
            ? CreatedAtAction(nameof(GetById),
                new { id = result.Data!.Id }, result)
            : BadRequest(result);
    }


    // POST api/reports/{id}/generate-pdf
    [HttpPost("{id}/generate-pdf")]
    [Authorize(Roles = "Specialist,Admin")]
    public async Task<IActionResult> GeneratePdf(int id)
    {
        var result = await _pdfService.GenerateReportPdfAsync(id);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    // GET api/reports/{id}/download
    [HttpGet("{id}/download")]
    [Authorize]
    public async Task<IActionResult> Download(int id)
    {
        var report = await _reportService.GetReportByIdAsync(id);
        if (!report.Success || report.Data?.ReportFilePath == null)
            return NotFound("الملف غير موجود، قم بتوليد الـ PDF أولاً");

        var fullPath = Path.Combine(
            _env.WebRootPath,
            report.Data.ReportFilePath.TrimStart('/'));

        if (!System.IO.File.Exists(fullPath))
            return NotFound("الملف غير موجود على السيرفر");

        var bytes = await System.IO.File.ReadAllBytesAsync(fullPath);
        return File(bytes, "application/pdf",
            $"report_{id}.pdf");
    }


    // GET api/reports/child/{childId}
    [HttpGet("child/{childId}")]
    public async Task<IActionResult> GetByChild(int childId)
    {
        var role = User.FindFirstValue(ClaimTypes.Role);

        // ولي الأمر يرى المنشورة فقط
        bool visibleOnly = role == "Parent";

        var result = await _reportService
            .GetReportsByChildAsync(childId, visibleOnly);
        return Ok(result);
    }

    // GET api/reports/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _reportService.GetReportByIdAsync(id);
        return result.Success ? Ok(result) : NotFound(result);
    }

    // POST api/reports/{id}/publish — نشر لولي الأمر
    [HttpPost("{id}/publish")]
    [Authorize(Roles = "Specialist")]
    public async Task<IActionResult> Publish(int id)
    {
        var specialistId = int.Parse(
            User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _reportService
            .PublishReportAsync(id, specialistId);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    // DELETE api/reports/{id}
    [HttpDelete("{id}")]
    [Authorize(Roles = "Specialist,Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _reportService.DeleteReportAsync(id);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    // GET api/reports/{id}/view
    [HttpGet("{id}/view")]
    [Authorize]
    public async Task<IActionResult> ViewReport(int id)
    {
        var report = await _reportService.GetReportByIdAsync(id);

        if (!report.Success || report.Data?.ReportFilePath == null)
            return NotFound("الملف غير موجود، قم بتوليد الـ PDF أولاً");

        var fullPath = Path.Combine(
            _env.WebRootPath,
            report.Data.ReportFilePath.TrimStart('/'));

        if (!System.IO.File.Exists(fullPath))
            return NotFound("الملف غير موجود على السيرفر");

        var bytes = await System.IO.File
            .ReadAllBytesAsync(fullPath);

        Response.Headers.Append(
            "Content-Disposition",
            $"inline; filename=\"report_{id}.pdf\"");

        return File(bytes, "application/pdf");
    }
}