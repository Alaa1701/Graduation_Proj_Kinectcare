namespace KinectCare.API.DTOs.Reports;

public class CreateReportDto
{
    public int SessionId { get; set; }
    public int ChildId { get; set; }
    public string? SessionObservations { get; set; }
    public string? ProgressAssessment { get; set; }
    public string? ChallengesNoticed { get; set; }
    public string? RecommendationsForParents { get; set; }
}