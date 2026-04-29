using KinectCare.API.DTOs.Common;
using KinectCare.API.DTOs.Dashboard;

namespace KinectCare.API.Services.Interfaces;

public interface IDashboardService
{
    Task<ApiResponse<DashboardStatsDto>> GetAdminStatsAsync();
    Task<ApiResponse<DashboardStatsDto>> GetSpecialistStatsAsync(int specialistId);
}