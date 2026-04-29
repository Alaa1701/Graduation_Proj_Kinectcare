namespace KinectCare.API.DTOs.Reports;

public class ReportDto
{
    public int Id { get; set; }
    public int SessionId { get; set; }
    public int ChildId { get; set; }
    public string ChildName { get; set; } = string.Empty;
    public int SpecialistId { get; set; }
    public string SpecialistName { get; set; } = string.Empty;
    public string? SessionObservations { get; set; }
    public string? ProgressAssessment { get; set; }
    public string? ChallengesNoticed { get; set; }
    public string? RecommendationsForParents { get; set; }
    public string? ReportFilePath { get; set; }
    public bool IsVisibleToParent { get; set; }
    public DateTime CreatedAt { get; set; }
    public string ExerciseType { get; set; } = string.Empty;
    public DateTime SessionDate { get; set; }
}