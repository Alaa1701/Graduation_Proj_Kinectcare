namespace KinectCare.API.DTOs.RehabPlans;

public class CreateRehabPlanDto
{
    public int ChildId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? WeeklyGoals { get; set; }
    public string? DailyExercises { get; set; }
    public string? Notes { get; set; }
}