using KinectCare.API.DTOs.Common;
using KinectCare.API.DTOs.Reports;

namespace KinectCare.API.Services.Interfaces;

public interface IReportService
{
    Task<ApiResponse<ReportDto>> CreateReportAsync(
        int specialistId, CreateReportDto dto);

    Task<ApiResponse<List<ReportDto>>> GetReportsByChildAsync(
        int childId, bool visibleOnly = false);

    Task<ApiResponse<ReportDto>> GetReportByIdAsync(int id);

    Task<ApiResponse<string>> PublishReportAsync(
        int reportId, int specialistId);

    Task<ApiResponse<string>> DeleteReportAsync(int id);
}