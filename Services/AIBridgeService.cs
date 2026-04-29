using System.Text.Json;
using KinectCare.API.Data;
using KinectCare.API.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

namespace KinectCare.API.Services;

public class AIBridgeService
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;
    private readonly FileStorageService _fileStorage;
    private readonly ILogger<AIBridgeService> _logger;

    public AIBridgeService(
        AppDbContext db,
        IConfiguration config,
        FileStorageService fileStorage,
        ILogger<AIBridgeService> logger)
    {
        _db = db;
        _config = config;
        _fileStorage = fileStorage;
        _logger = logger;
    }

    public async Task AnalyzeSessionAsync(int sessionId)
    {
        var session = await _db.Sessions.FindAsync(sessionId);
        if (session == null) return;

        try
        {
            // غيّر الـ Status إلى Analyzing
            session.Status = "Analyzing";
            await _db.SaveChangesAsync();

            // المسار الكامل للفيديو على السيرفر
            var videoFullPath = _fileStorage
                .GetFullPath(session.VideoPath!);

            // استدعاء Python Service
            var aiBaseUrl = _config["AIService:BaseUrl"];
            using var http = new HttpClient();
            http.Timeout = TimeSpan.FromMinutes(10);

            var response = await http.PostAsJsonAsync(
                $"{aiBaseUrl}/analyze",
                new { video_path = videoFullPath });

            if (!response.IsSuccessStatusCode)
            {
                await MarkAsFailedAsync(session,
                    "Python AI Service أرجع خطأ");
                return;
            }

            var json = await response.Content
                .ReadAsStringAsync();
            var aiResult = JsonSerializer.Deserialize
                <AIResponseDto>(json,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            if (aiResult == null)
            {
                await MarkAsFailedAsync(session,
                    "فشل في قراءة نتيجة التحليل");
                return;
            }

            // احفظ النتيجة في الـ DB
            var analysis = new AIAnalysisResult
            {
                SessionId = sessionId,
                PoseScore = aiResult.PoseScore,
                HandScore = aiResult.HandScore,
                ActivityLevel = aiResult.ActivityLevel,
                AttentionScore = aiResult.AttentionScore,
                MovementDetails = JsonSerializer.Serialize(
                    aiResult.MovementDetails),
                AIObservations = JsonSerializer.Serialize(
                    aiResult.Observations),
                OverallSummary = aiResult.Summary,
                IsApprovedBySpecialist = false,
                AnalyzedAt = DateTime.UtcNow
            };

            _db.AIAnalysisResults.Add(analysis);
            session.Status = "Done";
            await _db.SaveChangesAsync();

            // أرسل إشعاراً للأخصائي
            await SendNotificationAsync(
                session.SpecialistId,
                "اكتمل تحليل الجلسة",
                $"تم تحليل جلسة بنجاح وهي جاهزة للمراجعة",
                "AIAnalysisDone",
                sessionId,
                "Session");

            _logger.LogInformation(
                "Session {Id} analyzed successfully.", sessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error analyzing session {Id}", sessionId);
            await MarkAsFailedAsync(session, ex.Message);
        }
    }

    private async Task MarkAsFailedAsync(
        Session session, string reason)
    {
        session.Status = "Failed";
        await _db.SaveChangesAsync();
        _logger.LogWarning(
            "Session {Id} analysis failed: {Reason}",
            session.Id, reason);
    }

    private async Task SendNotificationAsync(
        int userId, string title, string message,
        string type, int entityId, string entityType)
    {
        var notification = new Notification
        {
            UserId = userId,
            Title = title,
            Message = message,
            Type = type,
            RelatedEntityId = entityId,
            RelatedEntityType = entityType,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };
        _db.Notifications.Add(notification);
        await _db.SaveChangesAsync();
    }
}

// DTO لاستقبال النتيجة من Python
public class AIResponseDto
{
    [JsonPropertyName("pose_score")]
    public float PoseScore { get; set; }

    [JsonPropertyName("hand_score")]
    public float HandScore { get; set; }

    [JsonPropertyName("activity_level")]
    public float ActivityLevel { get; set; }

    [JsonPropertyName("attention_score")]
    public float AttentionScore { get; set; }

    [JsonPropertyName("movement_details")]
    public object? MovementDetails { get; set; }

    [JsonPropertyName("observations")]
    public object? Observations { get; set; }

    [JsonPropertyName("summary")]
    public string Summary { get; set; } = string.Empty;
}