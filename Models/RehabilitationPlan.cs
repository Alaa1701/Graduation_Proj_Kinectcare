namespace KinectCare.API.Models;

public class RehabilitationPlan
{
    public int Id { get; set; }
    public int ChildId { get; set; }
    public int SpecialistId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? WeeklyGoals { get; set; }
    public string? DailyExercises { get; set; }  // JSON string
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string? PlanFilePath { get; set; }

    // Navigation Properties
    public Child Child { get; set; } = null!;
    public User Specialist { get; set; } = null!;
}