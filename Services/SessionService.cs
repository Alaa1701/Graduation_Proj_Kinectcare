using KinectCare.API.Data;
using KinectCare.API.DTOs.Common;
using KinectCare.API.DTOs.Sessions;
using KinectCare.API.Models;
using KinectCare.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Hangfire;

namespace KinectCare.API.Services;

public class SessionService : ISessionService
{
    private readonly AppDbContext _db;
    private readonly FileStorageService _fileStorage;
    private readonly IBackgroundJobClient _jobs;
    private readonly ILogger<SessionService> _logger;

    public SessionService(
        AppDbContext db,
        FileStorageService fileStorage,
        IBackgroundJobClient jobs,
        ILogger<SessionService> logger)
    {
        _db = db;
        _fileStorage = fileStorage;
        _jobs = jobs;
        _logger = logger;
    }

    public async Task<ApiResponse<SessionDto>> CreateSessionAsync(
        int specialistId,
        CreateSessionDto dto,
        IFormFile video)
    {

        var child = await _db.Children.FirstOrDefaultAsync(c =>
            c.Id == dto.ChildId &&
            c.SpecialistId == specialistId);

        if (child == null)
            return ApiResponse<SessionDto>.Fail(
                "الطفل غير موجود أو غير مرتبط بك");

        // تحقق من نوع الملف
        var allowedTypes = new[]
        {
            "video/mp4", "video/avi",
            "video/mov", "video/quicktime"
        };
        if (!allowedTypes.Contains(video.ContentType.ToLower()))
            return ApiResponse<SessionDto>.Fail(
                "نوع الملف غير مدعوم. يرجى رفع فيديو MP4 أو AVI");

        // تحقق من حجم الملف (500 MB max)
        if (video.Length > 500 * 1024 * 1024)
            return ApiResponse<SessionDto>.Fail(
                "حجم الفيديو كبير جداً. الحد الأقصى 500 MB");

        // حفظ الفيديو
        var videoPath = await _fileStorage.SaveVideoAsync(video);

        // إنشاء الجلسة
        var session = new Session
        {
            ChildId = dto.ChildId,
            SpecialistId = specialistId,
            ExerciseType = dto.ExerciseType,
            SessionDate = dto.SessionDate,
            VideoPath = videoPath,
            TherapistNotes = dto.TherapistNotes,
            Status = "Pending",
            CreatedAt = DateTime.UtcNow
        };

        _db.Sessions.Add(session);
        await _db.SaveChangesAsync();

        // أطلق Background Job للتحليل
        _jobs.Enqueue<AIBridgeService>(ai =>
            ai.AnalyzeSessionAsync(session.Id));

        _logger.LogInformation(
            "Session {Id} created, AI analysis queued.",
            session.Id);

        // أعد تحميل مع navigation properties
        await _db.Entry(session)
            .Reference(s => s.Child).LoadAsync();
        await _db.Entry(session)
            .Reference(s => s.Specialist).LoadAsync();

        return ApiResponse<SessionDto>.Ok(
            ToDto(session),
            "تم رفع الفيديو بنجاح وبدأ التحليل");
    }

    public async Task<ApiResponse<List<SessionDto>>>
        GetSessionsByChildAsync(int childId)
    {
        var sessions = await _db.Sessions
            .Include(s => s.Child)
            .Include(s => s.Specialist)
            .Include(s => s.AIAnalysisResult)
            .Where(s => s.ChildId == childId)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();

        return ApiResponse<List<SessionDto>>.Ok(
            sessions.Select(ToDto).ToList());
    }

    public async Task<ApiResponse<List<SessionDto>>>
        GetSessionsBySpecialistAsync(int specialistId)
    {
        var sessions = await _db.Sessions
            .Include(s => s.Child)
            .Include(s => s.Specialist)
            .Include(s => s.AIAnalysisResult)
            .Where(s => s.SpecialistId == specialistId)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();

        return ApiResponse<List<SessionDto>>.Ok(
            sessions.Select(ToDto).ToList());
    }

    public async Task<ApiResponse<SessionDto>>
        GetSessionByIdAsync(int id)
    {
        var session = await _db.Sessions
            .Include(s => s.Child)
            .Include(s => s.Specialist)
            .Include(s => s.AIAnalysisResult)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (session == null)
            return ApiResponse<SessionDto>.Fail(
                "الجلسة غير موجودة");

        return ApiResponse<SessionDto>.Ok(ToDto(session));
    }

    public async Task<ApiResponse<AIAnalysisResultDto>>
        GetAnalysisAsync(int sessionId)
    {
        var analysis = await _db.AIAnalysisResults
            .Include(a => a.Session)
            .ThenInclude(s => s.Child)
            .FirstOrDefaultAsync(a => a.SessionId == sessionId);

        if (analysis == null)
            return ApiResponse<AIAnalysisResultDto>.Fail(
                "نتيجة التحليل غير موجودة بعد");

        return ApiResponse<AIAnalysisResultDto>.Ok(
            new AIAnalysisResultDto
            {
                Id = analysis.Id,
                SessionId = analysis.SessionId,
                PoseScore = analysis.PoseScore,
                HandScore = analysis.HandScore,
                ActivityLevel = analysis.ActivityLevel,
                AttentionScore = analysis.AttentionScore,
                MovementDetails = analysis.MovementDetails,
                AIObservations = analysis.AIObservations,
                OverallSummary = analysis.OverallSummary,
                IsApprovedBySpecialist =
                    analysis.IsApprovedBySpecialist,
                AnalyzedAt = analysis.AnalyzedAt,
                ChildName = analysis.Session.Child.FullName,
                ExerciseType = analysis.Session.ExerciseType,
                SessionDate = analysis.Session.SessionDate
            });
    }

    public async Task<ApiResponse<string>> ApproveAnalysisAsync(
        int sessionId, int specialistId)
    {
        var analysis = await _db.AIAnalysisResults
            .Include(a => a.Session)
            .FirstOrDefaultAsync(a =>
                a.SessionId == sessionId &&
                a.Session.SpecialistId == specialistId);

        if (analysis == null)
            return ApiResponse<string>.Fail(
                "نتيجة التحليل غير موجودة");

        analysis.IsApprovedBySpecialist = true;
        await _db.SaveChangesAsync();

        return ApiResponse<string>.Ok(
            "تمت الموافقة على نتيجة التحليل وأصبحت مرئية لولي الأمر");
    }

    public async Task<ApiResponse<string>> DeleteSessionAsync(
        int id)
    {
        var session = await _db.Sessions.FindAsync(id);
        if (session == null)
            return ApiResponse<string>.Fail(
                "الجلسة غير موجودة");

        // احذف الفيديو من الـ storage
        _fileStorage.DeleteFile(session.VideoPath);

        _db.Sessions.Remove(session);
        await _db.SaveChangesAsync();

        return ApiResponse<string>.Ok("تم حذف الجلسة بنجاح");
    }

    public async Task<ApiResponse<string>> ReAnalyzeSessionAsync(
    int sessionId, int specialistId)
    {
        // تحقق أن الجلسة موجودة وتابعة لهذا الأخصائي
        var session = await _db.Sessions
            .Include(s => s.AIAnalysisResult)
            .FirstOrDefaultAsync(s =>
                s.Id == sessionId &&
                s.SpecialistId == specialistId);

        if (session == null)
            return ApiResponse<string>.Fail(
                "الجلسة غير موجودة أو غير مرتبطة بك");

        if (string.IsNullOrEmpty(session.VideoPath))
            return ApiResponse<string>.Fail(
                "لا يوجد فيديو مرتبط بهذه الجلسة");

        // احذف التحليل القديم إن وجد
        if (session.AIAnalysisResult != null)
        {
            _db.AIAnalysisResults.Remove(session.AIAnalysisResult);
        }

        // أعد تعيين الحالة
        session.Status = "Pending";
        await _db.SaveChangesAsync();

        // أطلق Background Job جديد
        _jobs.Enqueue<AIBridgeService>(ai =>
            ai.AnalyzeSessionAsync(session.Id));

        _logger.LogInformation(
            "Session {Id} re-queued for analysis.", sessionId);

        return ApiResponse<string>.Ok(
            "تمت إعادة إرسال الجلسة للتحليل بنجاح");
    }

    private static SessionDto ToDto(Session s) => new()
    {
        Id = s.Id,
        ChildId = s.ChildId,
        ChildName = s.Child?.FullName ?? "",
        SpecialistId = s.SpecialistId,
        SpecialistName = s.Specialist?.FullName ?? "",
        ExerciseType = s.ExerciseType,
        SessionDate = s.SessionDate,
        VideoPath = s.VideoPath,
        Status = s.Status,
        TherapistNotes = s.TherapistNotes,
        CreatedAt = s.CreatedAt,
        HasAnalysis = s.AIAnalysisResult != null,
        IsApproved = s.AIAnalysisResult?
            .IsApprovedBySpecialist ?? false
    };
}