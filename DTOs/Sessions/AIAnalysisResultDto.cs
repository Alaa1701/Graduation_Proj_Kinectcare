namespace KinectCare.API.DTOs.Sessions;

public class AIAnalysisResultDto
{
    public int Id { get; set; }
    public int SessionId { get; set; }
    public float PoseScore { get; set; }
    public float HandScore { get; set; }
    public float ActivityLevel { get; set; }
    public float AttentionScore { get; set; }
    public string? MovementDetails { get; set; }
    public string? AIObservations { get; set; }
    public string? OverallSummary { get; set; }
    public bool IsApprovedBySpecialist { get; set; }
    public DateTime AnalyzedAt { get; set; }

    // بيانات الجلسة معها
    public string ChildName { get; set; } = string.Empty;
    public string ExerciseType { get; set; } = string.Empty;
    public DateTime SessionDate { get; set; }
}