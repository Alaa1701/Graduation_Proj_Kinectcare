namespace KinectCare.API.Models;

public class Session
{
    public int Id { get; set; }
    public int ChildId { get; set; }
    public int SpecialistId { get; set; }
    public string ExerciseType { get; set; } = string.Empty;
    public DateTime SessionDate { get; set; }
    public string? VideoPath { get; set; }
    // Pending | Analyzing | Done | Failed
    public string Status { get; set; } = "Pending";
    public string? TherapistNotes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation Properties
    public Child Child { get; set; } = null!;
    public User Specialist { get; set; } = null!;
    public AIAnalysisResult? AIAnalysisResult { get; set; }
    public ICollection<Report> Reports { get; set; } = new List<Report>();
}