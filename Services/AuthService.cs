using KinectCare.API.Data;
using KinectCare.API.DTOs.Auth;
using KinectCare.API.DTOs.Common;
using KinectCare.API.Helpers;
using KinectCare.API.Models;
using KinectCare.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KinectCare.API.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly JwtHelper _jwt;
    private readonly ILogger<AuthService> _logger;
    private readonly IEmailService _emailService; 


    public AuthService(AppDbContext db, JwtHelper jwt,
        ILogger<AuthService> logger, IEmailService emailService)
    {
        _db = db;
        _jwt = jwt;
        _logger = logger;
        _emailService = emailService;
    }

    // ── Login ─────────────────────────────────────────────
    public async Task<ApiResponse<LoginResponseDto>> LoginAsync(
        LoginRequestDto dto)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u =>
                u.Email.ToLower() == dto.Email.ToLower());

        if (user == null || !BCrypt.Net.BCrypt.Verify(
            dto.Password, user.PasswordHash))
            return ApiResponse<LoginResponseDto>.Fail(
                "البريد الإلكتروني أو كلمة المرور غير صحيحة");

        if (!user.IsActive)
            return ApiResponse<LoginResponseDto>.Fail(
                "هذا الحساب معطّل، يرجى التواصل مع الإدارة");

        var token = _jwt.GenerateToken(user);

        return ApiResponse<LoginResponseDto>.Ok(new LoginResponseDto
        {
            Token = token,
            Role = user.Role,
            FullName = user.FullName,
            Email = user.Email,
            UserId = user.Id
        }, "تم تسجيل الدخول بنجاح");
    }


    public async Task<ApiResponse<string>> ForgotPasswordAsync(
       ForgotPasswordDto dto)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u =>
            u.Email.ToLower() == dto.EmailOrPhone.ToLower() ||
            u.PhoneNumber == dto.EmailOrPhone);

        if (user == null)
            return ApiResponse<string>.Fail(
                "لا يوجد حساب مرتبط بهذا البريد أو رقم الهاتف");

        // احذف أي OTP قديم
        var oldOtps = _db.OtpTokens.Where(o =>
            o.EmailOrPhone == dto.EmailOrPhone && !o.IsUsed);
        _db.OtpTokens.RemoveRange(oldOtps);

        // أنشئ OTP جديد
        var code = new Random().Next(100000, 999999).ToString();
        var otp = new OtpToken
        {
            EmailOrPhone = dto.EmailOrPhone,
            Code = code,
            ExpiresAt = DateTime.UtcNow.AddMinutes(5),
            CreatedAt = DateTime.UtcNow
        };

        _db.OtpTokens.Add(otp);
        await _db.SaveChangesAsync();


        var isEmail = dto.EmailOrPhone.Contains('@');

        if (isEmail)
        {

            try
            {
                await _emailService.SendOtpEmailAsync(
                    dto.EmailOrPhone, code);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to send OTP email");
                return ApiResponse<string>.Fail(
                    "فشل في إرسال البريد الإلكتروني، " +
                    "تحقق من صحة البريد وحاول مرة أخرى");
            }
        }
        else
        {
            
            _logger.LogWarning(
                "SMS not configured. OTP for {Phone}: {Code}",
                dto.EmailOrPhone, code);


            return ApiResponse<string>.Ok(
                dto.EmailOrPhone,
                $"[DEV] رمز التحقق هو: {code} " +
                "(SMS غير مفعّل — للتطوير فقط)");
        }

        return ApiResponse<string>.Ok(
            dto.EmailOrPhone,
            "تم إرسال رمز التحقق إلى بريدك الإلكتروني");
    }


    public async Task<ApiResponse<string>> VerifyOtpAsync(
        VerifyOtpDto dto)
    {
        var otp = await _db.OtpTokens
            .Where(o =>
                o.EmailOrPhone == dto.EmailOrPhone &&
                o.Code == dto.OtpCode &&
                !o.IsUsed &&
                o.ExpiresAt > DateTime.UtcNow)
            .FirstOrDefaultAsync();

        if (otp == null)
            return ApiResponse<string>.Fail(
                "رمز التحقق غير صحيح أو منتهي الصلاحية");

        return ApiResponse<string>.Ok(
            "تم التحقق بنجاح",
            "رمز التحقق صحيح");
    }

    public async Task<ApiResponse<string>> ResetPasswordAsync(
        ResetPasswordDto dto)
    {
        if (dto.NewPassword != dto.ConfirmPassword)
            return ApiResponse<string>.Fail(
                "كلمة المرور وتأكيدها غير متطابقتين");

        var otp = await _db.OtpTokens
            .Where(o =>
                o.EmailOrPhone == dto.EmailOrPhone &&
                o.Code == dto.OtpCode &&
                !o.IsUsed &&
                o.ExpiresAt > DateTime.UtcNow)
            .FirstOrDefaultAsync();

        if (otp == null)
            return ApiResponse<string>.Fail(
                "رمز التحقق غير صحيح أو منتهي الصلاحية");

        var user = await _db.Users.FirstOrDefaultAsync(u =>
            u.Email.ToLower() == dto.EmailOrPhone.ToLower() ||
            u.PhoneNumber == dto.EmailOrPhone);

        if (user == null)
            return ApiResponse<string>.Fail(
                "المستخدم غير موجود");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(
            dto.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;
        otp.IsUsed = true;

        await _db.SaveChangesAsync();

        return ApiResponse<string>.Ok(
            "تم تغيير كلمة المرور بنجاح");
    }

    public async Task<ApiResponse<string>> ChangePasswordAsync(
        int userId, ChangePasswordDto dto)
    {
        if (dto.NewPassword != dto.ConfirmPassword)
            return ApiResponse<string>.Fail(
                "كلمة المرور الجديدة وتأكيدها غير متطابقتين");

        var user = await _db.Users.FindAsync(userId);
        if (user == null)
            return ApiResponse<string>.Fail(
                "المستخدم غير موجود");

        if (!BCrypt.Net.BCrypt.Verify(
            dto.CurrentPassword, user.PasswordHash))
            return ApiResponse<string>.Fail(
                "كلمة المرور الحالية غير صحيحة");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(
            dto.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return ApiResponse<string>.Ok(
            "تم تغيير كلمة المرور بنجاح");
    }
}














































// ── Forgot Password ───────────────────────────────────
//    public async Task<ApiResponse<string>> ForgotPasswordAsync(
//        ForgotPasswordDto dto)
//    {
//        var user = await _db.Users.FirstOrDefaultAsync(u =>
//            u.Email.ToLower() == dto.EmailOrPhone.ToLower() ||
//            u.PhoneNumber == dto.EmailOrPhone);

//        if (user == null)
//            return ApiResponse<string>.Fail(
//                "لا يوجد حساب مرتبط بهذا البريد أو رقم الهاتف");

//        // احذف أي OTP قديم لنفس المستخدم
//        var oldOtps = _db.OtpTokens.Where(o =>
//            o.EmailOrPhone == dto.EmailOrPhone && !o.IsUsed);
//        _db.OtpTokens.RemoveRange(oldOtps);

//        // أنشئ OTP جديد
//        var code = new Random().Next(100000, 999999).ToString();
//        var otp = new OtpToken
//        {
//            EmailOrPhone = dto.EmailOrPhone,
//            Code = code,
//            ExpiresAt = DateTime.UtcNow.AddMinutes(10),
//            CreatedAt = DateTime.UtcNow
//        };

//        _db.OtpTokens.Add(otp);
//        await _db.SaveChangesAsync();

//        // TODO: إرسال OTP عبر Email/SMS
//        // سنضيف EmailService لاحقاً
//        _logger.LogInformation(
//            "OTP for {Contact}: {Code}", dto.EmailOrPhone, code);

//        return ApiResponse<string>.Ok(
//            "تم إرسال رمز التحقق بنجاح",
//            "تم إرسال رمز التحقق إلى بريدك الإلكتروني أو هاتفك");
//    }

//    // ── Verify OTP ────────────────────────────────────────
//    public async Task<ApiResponse<string>> VerifyOtpAsync(
//        VerifyOtpDto dto)
//    {
//        var otp = await _db.OtpTokens
//            .Where(o =>
//                o.EmailOrPhone == dto.EmailOrPhone &&
//                o.Code == dto.OtpCode &&
//                !o.IsUsed &&
//                o.ExpiresAt > DateTime.UtcNow)
//            .FirstOrDefaultAsync();

//        if (otp == null)
//            return ApiResponse<string>.Fail(
//                "رمز التحقق غير صحيح أو منتهي الصلاحية");

//        return ApiResponse<string>.Ok(
//            "تم التحقق بنجاح",
//            "رمز التحقق صحيح، يمكنك الآن تعيين كلمة مرور جديدة");
//    }

//    // ── Reset Password ────────────────────────────────────
//    public async Task<ApiResponse<string>> ResetPasswordAsync(
//        ResetPasswordDto dto)
//    {
//        if (dto.NewPassword != dto.ConfirmPassword)
//            return ApiResponse<string>.Fail(
//                "كلمة المرور وتأكيدها غير متطابقتين");

//        var otp = await _db.OtpTokens
//            .Where(o =>
//                o.EmailOrPhone == dto.EmailOrPhone &&
//                o.Code == dto.OtpCode &&
//                !o.IsUsed &&
//                o.ExpiresAt > DateTime.UtcNow)
//            .FirstOrDefaultAsync();

//        if (otp == null)
//            return ApiResponse<string>.Fail(
//                "رمز التحقق غير صحيح أو منتهي الصلاحية");

//        var user = await _db.Users.FirstOrDefaultAsync(u =>
//            u.Email.ToLower() == dto.EmailOrPhone.ToLower() ||
//            u.PhoneNumber == dto.EmailOrPhone);

//        if (user == null)
//            return ApiResponse<string>.Fail("المستخدم غير موجود");

//        // تحديث كلمة المرور
//        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(
//            dto.NewPassword);
//        user.UpdatedAt = DateTime.UtcNow;

//        // تعليم الـ OTP كمستخدم
//        otp.IsUsed = true;

//        await _db.SaveChangesAsync();

//        return ApiResponse<string>.Ok(
//            "تم تغيير كلمة المرور بنجاح",
//            "يمكنك الآن تسجيل الدخول بكلمة المرور الجديدة");
//    }

//    // ── Change Password (للمستخدم المسجّل دخوله) ──────────
//    public async Task<ApiResponse<string>> ChangePasswordAsync(
//        int userId, ChangePasswordDto dto)
//    {
//        if (dto.NewPassword != dto.ConfirmPassword)
//            return ApiResponse<string>.Fail(
//                "كلمة المرور الجديدة وتأكيدها غير متطابقتين");

//        var user = await _db.Users.FindAsync(userId);
//        if (user == null)
//            return ApiResponse<string>.Fail("المستخدم غير موجود");

//        if (!BCrypt.Net.BCrypt.Verify(
//            dto.CurrentPassword, user.PasswordHash))
//            return ApiResponse<string>.Fail(
//                "كلمة المرور الحالية غير صحيحة");

//        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(
//            dto.NewPassword);
//        user.UpdatedAt = DateTime.UtcNow;

//        await _db.SaveChangesAsync();

//        return ApiResponse<string>.Ok(
//            "تم تغيير كلمة المرور بنجاح");
//    }
//}


