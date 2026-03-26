namespace KinectCare.API.Models;

public class ParentPermission
{
    public int Id { get; set; }
    public int ParentId { get; set; }
    public bool CanViewReports { get; set; } = true;
    public bool CanDownloadReports { get; set; } = true;
    public bool CanViewPlans { get; set; } = true;
    public bool ReceiveNotifications { get; set; } = true;

    // Navigation Property
    public User Parent { get; set; } = null!;
}