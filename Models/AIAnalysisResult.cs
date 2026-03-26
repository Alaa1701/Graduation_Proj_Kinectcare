namespace KinectCare.API.Models;

public class AIAnalysisResult
{
    public int Id { get; set; }
    public int SessionId { get; set; }
    public float PoseScore { get; set; }
    public float HandScore { get; set; }
    public float ActivityLevel { get; set; }
    public float AttentionScore { get; set; }
    public string? MovementDetails { get; set; }  // JSON string
    public string? AIObservations { get; set; }   // JSON string
    public string? OverallSummary { get; set; }
    public bool IsApprovedBySpecialist { get; set; } = false;
    public DateTime AnalyzedAt { get; set; } = DateTime.UtcNow;

    // Navigation Property
    public Session Session { get; set; } = null!;
}