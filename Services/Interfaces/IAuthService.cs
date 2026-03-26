using KinectCare.API.DTOs.Auth;
using KinectCare.API.DTOs.Common;

namespace KinectCare.API.Services.Interfaces;

public interface IAuthService
{
    Task<ApiResponse<LoginResponseDto>> LoginAsync(LoginRequestDto dto);
    Task<ApiResponse<string>> ForgotPasswordAsync(ForgotPasswordDto dto);
    Task<ApiResponse<string>> VerifyOtpAsync(VerifyOtpDto dto);
    Task<ApiResponse<string>> ResetPasswordAsync(ResetPasswordDto dto);
    Task<ApiResponse<string>> ChangePasswordAsync(
        int userId, ChangePasswordDto dto);
}