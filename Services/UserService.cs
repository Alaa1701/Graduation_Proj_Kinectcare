using KinectCare.API.Data;
using KinectCare.API.DTOs.Common;
using KinectCare.API.DTOs.Users;
using KinectCare.API.Models;
using KinectCare.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KinectCare.API.Services;

public class UserService : IUserService
{
    private readonly AppDbContext _db;

    public UserService(AppDbContext db)
    {
        _db = db;
    }

    // ── Specialists ───────────────────────────────────────

    public async Task<ApiResponse<List<UserDto>>>
        GetAllSpecialistsAsync()
    {
        var specialists = await _db.Users
            .Where(u => u.Role == "Specialist")
            .OrderByDescending(u => u.CreatedAt)
            .Select(u => ToDto(u))
            .ToListAsync();

        return ApiResponse<List<UserDto>>.Ok(specialists);
    }

    public async Task<ApiResponse<UserDto>>
        GetSpecialistByIdAsync(int id)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u =>
                u.Id == id && u.Role == "Specialist");

        if (user == null)
            return ApiResponse<UserDto>.Fail(
                "الأخصائي غير موجود");

        return ApiResponse<UserDto>.Ok(ToDto(user));
    }

    public async Task<ApiResponse<UserDto>> CreateSpecialistAsync(
        CreateSpecialistDto dto)
    {
        if (await _db.Users.AnyAsync(
            u => u.Email.ToLower() == dto.Email.ToLower()))
            return ApiResponse<UserDto>.Fail(
                "البريد الإلكتروني مستخدم بالفعل");

        var user = new User
        {
            FullName = dto.FullName,
            Email = dto.Email.ToLower(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(
                dto.Password),
            PhoneNumber = dto.PhoneNumber,
            Role = "Specialist",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return ApiResponse<UserDto>.Ok(
            ToDto(user), "تم إنشاء حساب الأخصائي بنجاح");
    }

    // ── Parents ───────────────────────────────────────────

    public async Task<ApiResponse<List<UserDto>>>
        GetAllParentsAsync()
    {
        var parents = await _db.Users
            .Where(u => u.Role == "Parent")
            .OrderByDescending(u => u.CreatedAt)
            .Select(u => ToDto(u))
            .ToListAsync();

        return ApiResponse<List<UserDto>>.Ok(parents);
    }

    public async Task<ApiResponse<UserDto>>
        GetParentByIdAsync(int id)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u =>
                u.Id == id && u.Role == "Parent");

        if (user == null)
            return ApiResponse<UserDto>.Fail(
                "ولي الأمر غير موجود");

        return ApiResponse<UserDto>.Ok(ToDto(user));
    }

    public async Task<ApiResponse<UserDto>> CreateParentAsync(
        CreateParentDto dto)
    {
        if (await _db.Users.AnyAsync(
            u => u.Email.ToLower() == dto.Email.ToLower()))
            return ApiResponse<UserDto>.Fail(
                "البريد الإلكتروني مستخدم بالفعل");

        var user = new User
        {
            FullName = dto.FullName,
            Email = dto.Email.ToLower(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(
                dto.Password),
            PhoneNumber = dto.PhoneNumber,
            Role = "Parent",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        // إنشاء الصلاحيات تلقائياً
        var permissions = new ParentPermission
        {
            ParentId = user.Id,
            CanViewReports = dto.CanViewReports,
            CanDownloadReports = dto.CanDownloadReports,
            CanViewPlans = dto.CanViewPlans,
            ReceiveNotifications = dto.ReceiveNotifications
        };

        _db.ParentPermissions.Add(permissions);
        await _db.SaveChangesAsync();

        return ApiResponse<UserDto>.Ok(
            ToDto(user), "تم إنشاء حساب ولي الأمر بنجاح");
    }

    // ── Shared ────────────────────────────────────────────

    public async Task<ApiResponse<UserDto>> UpdateUserAsync(
        int id, UpdateUserDto dto)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null)
            return ApiResponse<UserDto>.Fail(
                "المستخدم غير موجود");

        user.FullName = dto.FullName;
        user.PhoneNumber = dto.PhoneNumber;
        user.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return ApiResponse<UserDto>.Ok(
            ToDto(user), "تم تحديث البيانات بنجاح");
    }

    public async Task<ApiResponse<string>> ToggleUserStatusAsync(
        int id)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null)
            return ApiResponse<string>.Fail(
                "المستخدم غير موجود");

        if (user.Role == "Admin")
            return ApiResponse<string>.Fail(
                "لا يمكن تعطيل حساب الـ Admin");

        user.IsActive = !user.IsActive;
        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        var status = user.IsActive ? "تفعيل" : "تعطيل";
        return ApiResponse<string>.Ok(
            $"تم {status} الحساب بنجاح");
    }

    public async Task<ApiResponse<string>> DeleteUserAsync(int id)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null)
            return ApiResponse<string>.Fail(
                "المستخدم غير موجود");

        if (user.Role == "Admin")
            return ApiResponse<string>.Fail(
                "لا يمكن حذف حساب الـ Admin");

        // تحقق: هل لديه أطفال مرتبطون؟
        var hasChildren = await _db.Children.AnyAsync(c =>
            c.SpecialistId == id || c.ParentId == id);

        if (hasChildren)
            return ApiResponse<string>.Fail(
                "لا يمكن حذف هذا المستخدم لأنه مرتبط بأطفال في النظام");

        _db.Users.Remove(user);
        await _db.SaveChangesAsync();

        return ApiResponse<string>.Ok("تم حذف المستخدم بنجاح");
    }

    public async Task<ApiResponse<string>> UpdatePermissionsAsync(
        int parentId, UpdatePermissionsDto dto)
    {
        var permissions = await _db.ParentPermissions
            .FirstOrDefaultAsync(p => p.ParentId == parentId);

        if (permissions == null)
            return ApiResponse<string>.Fail(
                "لم يتم العثور على صلاحيات لهذا الحساب");

        permissions.CanViewReports = dto.CanViewReports;
        permissions.CanDownloadReports = dto.CanDownloadReports;
        permissions.CanViewPlans = dto.CanViewPlans;
        permissions.ReceiveNotifications = dto.ReceiveNotifications;

        await _db.SaveChangesAsync();
        return ApiResponse<string>.Ok(
            "تم تحديث الصلاحيات بنجاح");
    }

    public async Task<ApiResponse<UserDto>> UpdateMyProfileAsync(
    int userId, UpdateUserDto dto)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user == null)
            return ApiResponse<UserDto>.Fail("المستخدم غير موجود");

        user.FullName = dto.FullName;
        user.PhoneNumber = dto.PhoneNumber;
        user.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return ApiResponse<UserDto>.Ok(ToDto(user), "تم تحديث البيانات بنجاح");
    }

    // ── Helper ────────────────────────────────────────────
    private static UserDto ToDto(User u) => new()
    {
        Id = u.Id,
        FullName = u.FullName,
        Email = u.Email,
        PhoneNumber = u.PhoneNumber,
        ProfileImagePath = u.ProfileImagePath,
        Role = u.Role,
        IsActive = u.IsActive,
        CreatedAt = u.CreatedAt
    };
}