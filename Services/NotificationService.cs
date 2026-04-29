using KinectCare.API.Data;
using KinectCare.API.DTOs.Common;
using KinectCare.API.DTOs.Notifications;
using KinectCare.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KinectCare.API.Services;

public class NotificationService : INotificationService
{
    private readonly AppDbContext _db;

    public NotificationService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ApiResponse<List<NotificationDto>>>
        GetMyNotificationsAsync(int userId)
    {
        var notifications = await _db.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(50)
            .Select(n => new NotificationDto
            {
                Id = n.Id,
                Type = n.Type,
                Title = n.Title,
                Message = n.Message,
                RelatedEntityId = n.RelatedEntityId,
                RelatedEntityType = n.RelatedEntityType,
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt
            })
            .ToListAsync();

        return ApiResponse<List<NotificationDto>>
            .Ok(notifications);
    }

    public async Task<ApiResponse<string>> MarkAsReadAsync(
        int notificationId, int userId)
    {
        var notification = await _db.Notifications
            .FirstOrDefaultAsync(n =>
                n.Id == notificationId &&
                n.UserId == userId);

        if (notification == null)
            return ApiResponse<string>.Fail(
                "الإشعار غير موجود");

        notification.IsRead = true;
        await _db.SaveChangesAsync();

        return ApiResponse<string>.Ok("تم تعليم الإشعار كمقروء");
    }

    public async Task<ApiResponse<string>> MarkAllAsReadAsync(
        int userId)
    {
        var unread = await _db.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync();

        foreach (var n in unread)
            n.IsRead = true;

        await _db.SaveChangesAsync();

        return ApiResponse<string>.Ok(
            $"تم تعليم {unread.Count} إشعار كمقروء");
    }

    public async Task<int> GetUnreadCountAsync(int userId)
    {
        return await _db.Notifications.CountAsync(n =>
            n.UserId == userId && !n.IsRead);
    }
}