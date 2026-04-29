using KinectCare.API.Data;
using KinectCare.API.DTOs.Common;
using KinectCare.API.DTOs.Reports;
using KinectCare.API.Models;
using KinectCare.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KinectCare.API.Services;

public class ReportService : IReportService
{
    private readonly AppDbContext _db;

    public ReportService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ApiResponse<ReportDto>> CreateReportAsync(
        int specialistId, CreateReportDto dto)
    {
        // تحقق أن الطفل مرتبط بهذا الأخصائي
        var child = await _db.Children.FirstOrDefaultAsync(c =>
            c.Id == dto.ChildId &&
            c.SpecialistId == specialistId);

        if (child == null)
            return ApiResponse<ReportDto>.Fail(
                "الطفل غير موجود أو غير مرتبط بك");

        // تحقق أن الجلسة موجودة
        var session = await _db.Sessions.FirstOrDefaultAsync(s =>
            s.Id == dto.SessionId &&
            s.ChildId == dto.ChildId);

        if (session == null)
            return ApiResponse<ReportDto>.Fail(
                "الجلسة غير موجودة");

        var report = new Report
        {
            SessionId = dto.SessionId,
            ChildId = dto.ChildId,
            SpecialistId = specialistId,
            SessionObservations = dto.SessionObservations,
            ProgressAssessment = dto.ProgressAssessment,
            ChallengesNoticed = dto.ChallengesNoticed,
            RecommendationsForParents =
                dto.RecommendationsForParents,
            IsVisibleToParent = false,
            CreatedAt = DateTime.UtcNow
        };

        _db.Reports.Add(report);
        await _db.SaveChangesAsync();

        await _db.Entry(report)
            .Reference(r => r.Child).LoadAsync();
        await _db.Entry(report)
            .Reference(r => r.Specialist).LoadAsync();
        await _db.Entry(report)
            .Reference(r => r.Session).LoadAsync();

        return ApiResponse<ReportDto>.Ok(
            ToDto(report), "تم إنشاء التقرير بنجاح");
    }

    public async Task<ApiResponse<List<ReportDto>>>
        GetReportsByChildAsync(
            int childId, bool visibleOnly = false)
    {
        var query = _db.Reports
            .Include(r => r.Child)
            .Include(r => r.Specialist)
            .Include(r => r.Session)
            .Where(r => r.ChildId == childId);

        if (visibleOnly)
            query = query.Where(r => r.IsVisibleToParent);

        var reports = await query
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        return ApiResponse<List<ReportDto>>.Ok(
            reports.Select(ToDto).ToList());
    }

    public async Task<ApiResponse<ReportDto>>
        GetReportByIdAsync(int id)
    {
        var report = await _db.Reports
            .Include(r => r.Child)
            .Include(r => r.Specialist)
            .Include(r => r.Session)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (report == null)
            return ApiResponse<ReportDto>.Fail(
                "التقرير غير موجود");

        return ApiResponse<ReportDto>.Ok(ToDto(report));
    }

    public async Task<ApiResponse<string>> PublishReportAsync(
        int reportId, int specialistId)
    {
        var report = await _db.Reports
            .Include(r => r.Child)
            .FirstOrDefaultAsync(r =>
                r.Id == reportId &&
                r.SpecialistId == specialistId);

        if (report == null)
            return ApiResponse<string>.Fail(
                "التقرير غير موجود");

        report.IsVisibleToParent = true;
        await _db.SaveChangesAsync();

        // أرسل إشعاراً لولي الأمر
        var notification = new Notification
        {
            UserId = report.Child.ParentId,
            Type = "NewReport",
            Title = "تقرير جديد متاح",
            Message = $"تم إضافة تقرير جديد للطفل " +
                      $"{report.Child.FullName}",
            RelatedEntityId = reportId,
            RelatedEntityType = "Report",
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        _db.Notifications.Add(notification);
        await _db.SaveChangesAsync();

        return ApiResponse<string>.Ok(
            "تم نشر التقرير وإشعار ولي الأمر بنجاح");
    }

    public async Task<ApiResponse<string>> DeleteReportAsync(
        int id)
    {
        var report = await _db.Reports.FindAsync(id);
        if (report == null)
            return ApiResponse<string>.Fail(
                "التقرير غير موجود");

        _db.Reports.Remove(report);
        await _db.SaveChangesAsync();

        return ApiResponse<string>.Ok("تم حذف التقرير بنجاح");
    }

    private static ReportDto ToDto(Report r) => new()
    {
        Id = r.Id,
        SessionId = r.SessionId,
        ChildId = r.ChildId,
        ChildName = r.Child?.FullName ?? "",
        SpecialistId = r.SpecialistId,
        SpecialistName = r.Specialist?.FullName ?? "",
        SessionObservations = r.SessionObservations,
        ProgressAssessment = r.ProgressAssessment,
        ChallengesNoticed = r.ChallengesNoticed,
        RecommendationsForParents = r.RecommendationsForParents,
        ReportFilePath = r.ReportFilePath,
        IsVisibleToParent = r.IsVisibleToParent,
        CreatedAt = r.CreatedAt,
        ExerciseType = r.Session?.ExerciseType ?? "",
        SessionDate = r.Session?.SessionDate ?? DateTime.MinValue
    };
}