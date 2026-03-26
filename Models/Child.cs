namespace KinectCare.API.Models;

public class Child
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public string Gender { get; set; } = string.Empty;
    public string DiagnosisType { get; set; } = string.Empty;
    public string? DiagnosisDetails { get; set; }
    public string? ProfileImagePath { get; set; }
    public string Status { get; set; } = "Active"; // Active | Completed | OnHold

    // Foreign Keys — طفل واحد لكل أخصائي وكل ولي أمر
    public int SpecialistId { get; set; }
    public int ParentId { get; set; }
    public int CreatedByAdminId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation Properties
    public User Specialist { get; set; } = null!;
    public User Parent { get; set; } = null!;
    public User CreatedByAdmin { get; set; } = null!;
    public ICollection<Session> Sessions { get; set; } = new List<Session>();
    public ICollection<Report> Reports { get; set; } = new List<Report>();
    public ICollection<RehabilitationPlan> RehabPlans { get; set; } = new List<RehabilitationPlan>();
}