using KinectCare.API.DTOs.Common;
using KinectCare.API.DTOs.Sessions;
using Microsoft.AspNetCore.Http;

namespace KinectCare.API.Services.Interfaces;

public interface ISessionService
{
    Task<ApiResponse<SessionDto>> CreateSessionAsync(
        int specialistId,
        CreateSessionDto dto,
        IFormFile video);

    Task<ApiResponse<List<SessionDto>>> GetSessionsByChildAsync(
        int childId);

    Task<ApiResponse<List<SessionDto>>> GetSessionsBySpecialistAsync(
        int specialistId);

    Task<ApiResponse<SessionDto>> GetSessionByIdAsync(int id);

    Task<ApiResponse<AIAnalysisResultDto>> GetAnalysisAsync(
        int sessionId);

    Task<ApiResponse<string>> ApproveAnalysisAsync(
        int sessionId, int specialistId);

    Task<ApiResponse<string>> DeleteSessionAsync(int id);

    Task<ApiResponse<string>> ReAnalyzeSessionAsync(
        int sessionId, int specialistId);
}