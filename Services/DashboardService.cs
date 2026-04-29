using KinectCare.API.Data;
using KinectCare.API.DTOs.Common;
using KinectCare.API.DTOs.Dashboard;
using KinectCare.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KinectCare.API.Services;

public class DashboardService : IDashboardService
{
    private readonly AppDbContext _db;

    public DashboardService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ApiResponse<DashboardStatsDto>>
        GetAdminStatsAsync()
    {
        var stats = new DashboardStatsDto
        {
            TotalChildren = await _db.Children.CountAsync(),
            TotalSpecialists = await _db.Users.CountAsync(
                u => u.Role == "Specialist" && u.IsActive),
            TotalParents = await _db.Users.CountAsync(
                u => u.Role == "Parent" && u.IsActive),
            ActiveSessions = await _db.Sessions.CountAsync(
                s => s.Status == "Analyzing" ||
                     s.Status == "Pending"),
            CompletedSessions = await _db.Sessions.CountAsync(
                s => s.Status == "Done"),
            RecentChildren = await _db.Children
                .Include(c => c.Specialist)
                .OrderByDescending(c => c.CreatedAt)
                .Take(5)
                .Select(c => new RecentChildDto
                {
                    Id = c.Id,
                    FullName = c.FullName,
                    Status = c.Status,
                    SpecialistName = c.Specialist.FullName,
                    CreatedAt = c.CreatedAt
                })
                .ToListAsync()
        };

        return ApiResponse<DashboardStatsDto>.Ok(stats);
    }

    public async Task<ApiResponse<DashboardStatsDto>>
        GetSpecialistStatsAsync(int specialistId)
    {
        var stats = new DashboardStatsDto
        {
            TotalChildren = await _db.Children.CountAsync(
                c => c.SpecialistId == specialistId),
            ActiveSessions = await _db.Sessions.CountAsync(
                s => s.SpecialistId == specialistId &&
                    (s.Status == "Analyzing" ||
                     s.Status == "Pending")),
            CompletedSessions = await _db.Sessions.CountAsync(
                s => s.SpecialistId == specialistId &&
                     s.Status == "Done"),
            RecentChildren = await _db.Children
                .Include(c => c.Specialist)
                .Where(c => c.SpecialistId == specialistId)
                .OrderByDescending(c => c.CreatedAt)
                .Take(5)
                .Select(c => new RecentChildDto
                {
                    Id = c.Id,
                    FullName = c.FullName,
                    Status = c.Status,
                    SpecialistName = c.Specialist.FullName,
                    CreatedAt = c.CreatedAt
                })
                .ToListAsync()
        };

        return ApiResponse<DashboardStatsDto>.Ok(stats);
    }
}