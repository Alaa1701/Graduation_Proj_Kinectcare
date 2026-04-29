namespace KinectCare.API.DTOs.Sessions;

public class SessionDto
{
    public int Id { get; set; }
    public int ChildId { get; set; }
    public string ChildName { get; set; } = string.Empty;
    public int SpecialistId { get; set; }
    public string SpecialistName { get; set; } = string.Empty;
    public string ExerciseType { get; set; } = string.Empty;
    public DateTime SessionDate { get; set; }
    public string? VideoPath { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? TherapistNotes { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool HasAnalysis { get; set; }
    public bool IsApproved { get; set; }
}