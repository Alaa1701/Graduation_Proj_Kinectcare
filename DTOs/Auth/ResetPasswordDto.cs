namespace KinectCare.API.DTOs.Auth;

public class ResetPasswordDto
{
    public string EmailOrPhone { get; set; } = string.Empty;
    public string OtpCode { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
}