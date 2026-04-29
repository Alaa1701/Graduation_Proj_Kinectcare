using KinectCare.API.Data;
using KinectCare.API.DTOs.Common;
using KinectCare.API.DTOs.RehabPlans;
using KinectCare.API.Models;
using KinectCare.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KinectCare.API.Services;

public class RehabPlanService : IRehabPlanService
{
    private readonly AppDbContext _db;

    public RehabPlanService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ApiResponse<RehabPlanDto>> CreatePlanAsync(
        int specialistId, CreateRehabPlanDto dto)
    {
        var child = await _db.Children.FirstOrDefaultAsync(c =>
            c.Id == dto.ChildId &&
            c.SpecialistId == specialistId);

        if (child == null)
            return ApiResponse<RehabPlanDto>.Fail(
                "الطفل غير موجود أو غير مرتبط بك");

        // عطّل الخطة القديمة تلقائياً
        var oldPlans = await _db.RehabilitationPlans
            .Where(p => p.ChildId == dto.ChildId && p.IsActive)
            .ToListAsync();

        foreach (var old in oldPlans)
        {
            old.IsActive = false;
            old.UpdatedAt = DateTime.UtcNow;
        }

        var plan = new RehabilitationPlan
        {
            ChildId = dto.ChildId,
            SpecialistId = specialistId,
            Title = dto.Title,
            WeeklyGoals = dto.WeeklyGoals,
            DailyExercises = dto.DailyExercises,
            Notes = dto.Notes,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.RehabilitationPlans.Add(plan);
        await _db.SaveChangesAsync();

        // أرسل إشعاراً لولي الأمر
        var notification = new Notification
        {
            UserId = child.ParentId,
            Type = "NewPlan",
            Title = "خطة تأهيلية جديدة",
            Message = $"تم إضافة خطة تأهيلية جديدة " +
                      $"للطفل {child.FullName}",
            RelatedEntityId = plan.Id,
            RelatedEntityType = "RehabPlan",
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        _db.Notifications.Add(notification);
        await _db.SaveChangesAsync();

        await _db.Entry(plan)
            .Reference(p => p.Child).LoadAsync();
        await _db.Entry(plan)
            .Reference(p => p.Specialist).LoadAsync();

        return ApiResponse<RehabPlanDto>.Ok(
            ToDto(plan), "تم إنشاء الخطة التأهيلية بنجاح");
    }

    public async Task<ApiResponse<List<RehabPlanDto>>>
        GetAllPlansByChildAsync(int childId)
    {
        var plans = await _db.RehabilitationPlans
            .Include(p => p.Child)
            .Include(p => p.Specialist)
            .Where(p => p.ChildId == childId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        return ApiResponse<List<RehabPlanDto>>.Ok(
            plans.Select(ToDto).ToList());
    }

    public async Task<ApiResponse<RehabPlanDto>>
        GetLatestPlanByChildAsync(int childId)
    {
        var plan = await _db.RehabilitationPlans
            .Include(p => p.Child)
            .Include(p => p.Specialist)
            .Where(p =>
                p.ChildId == childId && p.IsActive)
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefaultAsync();

        if (plan == null)
            return ApiResponse<RehabPlanDto>.Fail(
                "لا توجد خطة تأهيلية حالياً");

        return ApiResponse<RehabPlanDto>.Ok(ToDto(plan));
    }

    public async Task<ApiResponse<string>> DeletePlanAsync(int id)
    {
        var plan = await _db.RehabilitationPlans.FindAsync(id);
        if (plan == null)
            return ApiResponse<string>.Fail(
                "الخطة غير موجودة");

        _db.RehabilitationPlans.Remove(plan);
        await _db.SaveChangesAsync();

        return ApiResponse<string>.Ok("تم حذف الخطة بنجاح");
    }

    private static RehabPlanDto ToDto(RehabilitationPlan p) =>
        new()
        {
            Id = p.Id,
            ChildId = p.ChildId,
            ChildName = p.Child?.FullName ?? "",
            SpecialistId = p.SpecialistId,
            SpecialistName = p.Specialist?.FullName ?? "",
            Title = p.Title,
            WeeklyGoals = p.WeeklyGoals,
            DailyExercises = p.DailyExercises,
            Notes = p.Notes,
            IsActive = p.IsActive,
            CreatedAt = p.CreatedAt,
            UpdatedAt = p.UpdatedAt,
            PlanFilePath = p.PlanFilePath  

        };
}