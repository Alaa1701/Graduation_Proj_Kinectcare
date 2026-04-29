using KinectCare.API.DTOs.Children;
using KinectCare.API.DTOs.Common;

namespace KinectCare.API.Services.Interfaces;

public interface IChildService
{
    Task<ApiResponse<List<ChildDto>>> GetAllChildrenAsync();
    Task<ApiResponse<List<ChildDto>>> GetChildrenBySpecialistAsync(
        int specialistId);
    Task<ApiResponse<List<ChildDto>>> GetChildrenByParentAsync(
        int parentId);
    Task<ApiResponse<ChildDto>> GetChildByIdAsync(int id);
    Task<ApiResponse<ChildDto>> CreateChildAsync(
        int adminId, CreateChildDto dto);
    Task<ApiResponse<ChildDto>> UpdateChildAsync(
        int id, UpdateChildDto dto);
    Task<ApiResponse<string>> DeleteChildAsync(int id);
}