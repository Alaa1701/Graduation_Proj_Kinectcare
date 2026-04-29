namespace KinectCare.API.DTOs.Users;

public class UpdatePermissionsDto
{
    public bool CanViewReports { get; set; }
    public bool CanDownloadReports { get; set; }
    public bool CanViewPlans { get; set; }
    public bool ReceiveNotifications { get; set; }
}