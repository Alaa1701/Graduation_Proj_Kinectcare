using KinectCare.API.DTOs.Common;
using KinectCare.API.DTOs.Users;

namespace KinectCare.API.Services.Interfaces;

public interface IUserService
{
    // Specialists
    Task<ApiResponse<List<UserDto>>> GetAllSpecialistsAsync();
    Task<ApiResponse<UserDto>> GetSpecialistByIdAsync(int id);
    Task<ApiResponse<UserDto>> CreateSpecialistAsync(
        CreateSpecialistDto dto);
    Task<ApiResponse<UserDto>> UpdateUserAsync(
        int id, UpdateUserDto dto);
    Task<ApiResponse<string>> ToggleUserStatusAsync(int id);
    Task<ApiResponse<string>> DeleteUserAsync(int id);

    // Parents
    Task<ApiResponse<List<UserDto>>> GetAllParentsAsync();
    Task<ApiResponse<UserDto>> GetParentByIdAsync(int id);
    Task<ApiResponse<UserDto>> CreateParentAsync(
        CreateParentDto dto);
    Task<ApiResponse<string>> UpdatePermissionsAsync(
        int parentId, UpdatePermissionsDto dto);

    Task<ApiResponse<UserDto>> UpdateMyProfileAsync(int userId, UpdateUserDto dto);
}