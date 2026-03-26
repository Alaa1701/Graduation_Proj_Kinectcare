namespace KinectCare.API.DTOs.Auth;

public class VerifyOtpDto
{
    public string EmailOrPhone { get; set; } = string.Empty;
    public string OtpCode { get; set; } = string.Empty;
}