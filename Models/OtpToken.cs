namespace KinectCare.API.Models;

public class OtpToken
{
    public int Id { get; set; }
    public string EmailOrPhone { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public bool IsUsed { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}