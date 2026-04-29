namespace KinectCare.API.DTOs.Sessions;

public class CreateSessionDto
{
    public int ChildId { get; set; }
    public string ExerciseType { get; set; } = string.Empty;
    public DateTime SessionDate { get; set; }
    public string? TherapistNotes { get; set; }
}