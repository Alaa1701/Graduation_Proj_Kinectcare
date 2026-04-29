using KinectCare.API.DTOs.Common;
using KinectCare.API.DTOs.RehabPlans;

namespace KinectCare.API.Services.Interfaces;

public interface IRehabPlanService
{
    Task<ApiResponse<RehabPlanDto>> CreatePlanAsync(
        int specialistId, CreateRehabPlanDto dto);

    Task<ApiResponse<List<RehabPlanDto>>> GetAllPlansByChildAsync(
        int childId);

    Task<ApiResponse<RehabPlanDto>> GetLatestPlanByChildAsync(
        int childId);

    Task<ApiResponse<string>> DeletePlanAsync(int id);
}