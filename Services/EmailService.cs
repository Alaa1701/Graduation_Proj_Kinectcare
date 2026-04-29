using System.Net;
using System.Net.Mail;
using KinectCare.API.Services.Interfaces;

namespace KinectCare.API.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<EmailService> _logger;

    public EmailService(
        IConfiguration config,
        ILogger<EmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task SendOtpEmailAsync(
        string toEmail, string otpCode)
    {
        var settings = _config.GetSection("EmailSettings");
        var host = settings["SmtpHost"]!;
        var port = int.Parse(settings["SmtpPort"]!);
        var sender = settings["SenderEmail"]!;
        var name = settings["SenderName"]!;
        var password = settings["Password"]!;

        using var client = new SmtpClient(host, port)
        {
            Credentials = new NetworkCredential(sender, password),
            EnableSsl = true,
        };

        var body = $"""
            <div dir="rtl" style="font-family:Arial;
                 max-width:500px;margin:auto;
                 background:#f9f9f9;padding:30px;
                 border-radius:12px;">

              <div style="text-align:center;margin-bottom:24px;">
                <h2 style="color:#1D9E75;margin:0;">
                  KinectCare
                </h2>
                <p style="color:#888;font-size:13px;margin:4px 0 0;">
                  نظام متابعة الأطفال ذوي الاحتياجات الخاصة
                </p>
              </div>

              <div style="background:white;padding:24px;
                   border-radius:8px;
                   border:1px solid #e5e7eb;">
                <h3 style="color:#1f2937;margin:0 0 12px;">
                  رمز التحقق الخاص بك
                </h3>
                <p style="color:#6b7280;font-size:14px;
                   margin:0 0 20px;">
                  استخدم الرمز التالي لإعادة تعيين كلمة المرور.
                  صالح لمدة <strong>10 دقائق</strong> فقط.
                </p>

                <div style="background:#E1F5EE;
                     border:2px dashed #1D9E75;
                     border-radius:8px;padding:16px;
                     text-align:center;margin-bottom:20px;">
                  <span style="font-size:36px;font-weight:bold;
                        letter-spacing:8px;color:#1D9E75;">
                    {otpCode}
                  </span>
                </div>

                <p style="color:#ef4444;font-size:12px;margin:0;">
                  ⚠️ لا تشارك هذا الرمز مع أي شخص.
                  إذا لم تطلب إعادة التعيين، تجاهل هذا البريد.
                </p>
              </div>

              <p style="text-align:center;color:#9ca3af;
                 font-size:11px;margin-top:16px;">
                © 2025 KinectCare — جميع الحقوق محفوظة
              </p>
            </div>
            """;

        var mail = new MailMessage
        {
            From = new MailAddress(sender, name),
            Subject = $"رمز التحقق KinectCare — {otpCode}",
            Body = body,
            IsBodyHtml = true,
        };
        mail.To.Add(toEmail);

        try
        {
            await client.SendMailAsync(mail);
            _logger.LogInformation(
                "OTP email sent to {Email}", toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to send OTP email to {Email}", toEmail);
            throw;
        }
    }
}