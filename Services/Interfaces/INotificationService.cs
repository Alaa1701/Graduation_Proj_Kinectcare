using KinectCare.API.DTOs.Common;
using KinectCare.API.DTOs.Notifications;

namespace KinectCare.API.Services.Interfaces;

public interface INotificationService
{
    Task<ApiResponse<List<NotificationDto>>>
        GetMyNotificationsAsync(int userId);

    Task<ApiResponse<string>> MarkAsReadAsync(
        int notificationId, int userId);

    Task<ApiResponse<string>> MarkAllAsReadAsync(int userId);

    Task<int> GetUnreadCountAsync(int userId);
}