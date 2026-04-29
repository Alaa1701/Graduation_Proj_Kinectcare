namespace KinectCare.API.DTOs.RehabPlans;

public class RehabPlanDto
{
    public int Id { get; set; }
    public int ChildId { get; set; }
    public string ChildName { get; set; } = string.Empty;
    public int SpecialistId { get; set; }
    public string SpecialistName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? WeeklyGoals { get; set; }
    public string? DailyExercises { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? PlanFilePath { get; set; }

}