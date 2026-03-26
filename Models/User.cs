using Org.BouncyCastle.Asn1.Ocsp;

namespace KinectCare.API.Models;

public class User
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? ProfileImagePath { get; set; }
    public string Role { get; set; } = string.Empty; // Admin | Specialist | Parent
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation Properties
    public ParentPermission? ParentPermission { get; set; }
    public ICollection<Child> ManagedChildren { get; set; } = new List<Child>();
    public ICollection<Child> ParentChildren { get; set; } = new List<Child>();
    public ICollection<Session> Sessions { get; set; } = new List<Session>();
    public ICollection<Report> Reports { get; set; } = new List<Report>();
    public ICollection<RehabilitationPlan> RehabPlans { get; set; } = new List<RehabilitationPlan>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
}