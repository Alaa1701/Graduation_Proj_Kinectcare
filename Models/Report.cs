namespace KinectCare.API.Models;

public class Report
{
    public int Id { get; set; }
    public int SessionId { get; set; }
    public int ChildId { get; set; }
    public int SpecialistId { get; set; }
    public string? SessionObservations { get; set; }
    public string? ProgressAssessment { get; set; }
    public string? ChallengesNoticed { get; set; }
    public string? RecommendationsForParents { get; set; }
    public string? ReportFilePath { get; set; }
    public bool IsVisibleToParent { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation Properties
    public Session Session { get; set; } = null!;
    public Child Child { get; set; } = null!;
    public User Specialist { get; set; } = null!;
}