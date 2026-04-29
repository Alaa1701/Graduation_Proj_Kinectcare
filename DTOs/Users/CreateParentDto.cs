namespace KinectCare.API.DTOs.Users;

public class CreateParentDto
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Relationship { get; set; } = string.Empty;

    // الصلاحيات
    public bool CanViewReports { get; set; } = true;
    public bool CanDownloadReports { get; set; } = true;
    public bool CanViewPlans { get; set; } = true;
    public bool ReceiveNotifications { get; set; } = true;
}