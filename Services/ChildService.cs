using KinectCare.API.Data;
using KinectCare.API.DTOs.Children;
using KinectCare.API.DTOs.Common;
using KinectCare.API.Models;
using KinectCare.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KinectCare.API.Services;

public class ChildService : IChildService
{
    private readonly AppDbContext _db;

    public ChildService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ApiResponse<List<ChildDto>>>
        GetAllChildrenAsync()
    {
        var children = await _db.Children
            .Include(c => c.Specialist)
            .Include(c => c.Parent)
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => ToDto(c))
            .ToListAsync();

        return ApiResponse<List<ChildDto>>.Ok(children);
    }

    public async Task<ApiResponse<List<ChildDto>>>
        GetChildrenBySpecialistAsync(int specialistId)
    {
        var children = await _db.Children
            .Include(c => c.Specialist)
            .Include(c => c.Parent)
            .Where(c => c.SpecialistId == specialistId)
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => ToDto(c))
            .ToListAsync();

        return ApiResponse<List<ChildDto>>.Ok(children);
    }

    public async Task<ApiResponse<List<ChildDto>>>
        GetChildrenByParentAsync(int parentId)
    {
        var children = await _db.Children
            .Include(c => c.Specialist)
            .Include(c => c.Parent)
            .Where(c => c.ParentId == parentId)
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => ToDto(c))
            .ToListAsync();

        return ApiResponse<List<ChildDto>>.Ok(children);
    }

    public async Task<ApiResponse<ChildDto>> GetChildByIdAsync(
        int id)
    {
        var child = await _db.Children
            .Include(c => c.Specialist)
            .Include(c => c.Parent)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (child == null)
            return ApiResponse<ChildDto>.Fail(
                "الطفل غير موجود");

        return ApiResponse<ChildDto>.Ok(ToDto(child));
    }

    public async Task<ApiResponse<ChildDto>> CreateChildAsync(
        int adminId, CreateChildDto dto)
    {
        // تحقق أن الأخصائي موجود
        var specialist = await _db.Users.FirstOrDefaultAsync(
            u => u.Id == dto.SpecialistId &&
                 u.Role == "Specialist" &&
                 u.IsActive);
        if (specialist == null)
            return ApiResponse<ChildDto>.Fail(
                "الأخصائي غير موجود أو غير نشط");

        // تحقق أن ولي الأمر موجود
        var parent = await _db.Users.FirstOrDefaultAsync(
            u => u.Id == dto.ParentId &&
                 u.Role == "Parent" &&
                 u.IsActive);
        if (parent == null)
            return ApiResponse<ChildDto>.Fail(
                "ولي الأمر غير موجود أو غير نشط");

        var child = new Child
        {
            FullName = dto.FullName,
            DateOfBirth = dto.DateOfBirth,
            Gender = dto.Gender,
            DiagnosisType = dto.DiagnosisType,
            DiagnosisDetails = dto.DiagnosisDetails,
            SpecialistId = dto.SpecialistId,
            ParentId = dto.ParentId,
            CreatedByAdminId = adminId,
            Status = "Active",
            CreatedAt = DateTime.UtcNow
        };

        _db.Children.Add(child);
        await _db.SaveChangesAsync();

        // أعد تحميل مع الـ navigation properties
        await _db.Entry(child)
            .Reference(c => c.Specialist).LoadAsync();
        await _db.Entry(child)
            .Reference(c => c.Parent).LoadAsync();

        return ApiResponse<ChildDto>.Ok(
            ToDto(child), "تم إضافة الطفل بنجاح");
    }

    public async Task<ApiResponse<ChildDto>> UpdateChildAsync(
        int id, UpdateChildDto dto)
    {
        var child = await _db.Children
            .Include(c => c.Specialist)
            .Include(c => c.Parent)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (child == null)
            return ApiResponse<ChildDto>.Fail(
                "الطفل غير موجود");

        child.FullName = dto.FullName;
        child.DateOfBirth = dto.DateOfBirth;
        child.Gender = dto.Gender;
        child.DiagnosisType = dto.DiagnosisType;
        child.DiagnosisDetails = dto.DiagnosisDetails;
        child.Status = dto.Status;
        child.SpecialistId = dto.SpecialistId;
        child.ParentId = dto.ParentId;

        await _db.SaveChangesAsync();

        // أعد تحميل navigation properties بعد التحديث
        await _db.Entry(child)
            .Reference(c => c.Specialist).LoadAsync();
        await _db.Entry(child)
            .Reference(c => c.Parent).LoadAsync();

        return ApiResponse<ChildDto>.Ok(
            ToDto(child), "تم تحديث بيانات الطفل بنجاح");
    }

    public async Task<ApiResponse<string>> DeleteChildAsync(int id)
    {
        var child = await _db.Children.FindAsync(id);
        if (child == null)
            return ApiResponse<string>.Fail(
                "الطفل غير موجود");

        _db.Children.Remove(child);
        await _db.SaveChangesAsync();

        return ApiResponse<string>.Ok("تم حذف الطفل بنجاح");
    }

    // ── Helper ────────────────────────────────────────────
    private static ChildDto ToDto(Child c) => new()
    {
        Id = c.Id,
        FullName = c.FullName,
        DateOfBirth = c.DateOfBirth,
        Gender = c.Gender,
        DiagnosisType = c.DiagnosisType,
        DiagnosisDetails = c.DiagnosisDetails,
        ProfileImagePath = c.ProfileImagePath,
        Status = c.Status,
        CreatedAt = c.CreatedAt,
        SpecialistId = c.SpecialistId,
        SpecialistName = c.Specialist?.FullName ?? "",
        ParentId = c.ParentId,
        ParentName = c.Parent?.FullName ?? ""
    };
}