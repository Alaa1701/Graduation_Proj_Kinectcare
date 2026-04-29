using System.Security.Claims;
using KinectCare.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KinectCare.API.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notifService;

    public NotificationsController(
        INotificationService notifService)
    {
        _notifService = notifService;
    }

    // GET api/notifications
    [HttpGet]
    public async Task<IActionResult> GetMy()
    {
        var userId = int.Parse(
            User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _notifService
            .GetMyNotificationsAsync(userId);
        return Ok(result);
    }

    // GET api/notifications/unread-count
    [HttpGet("unread-count")]
    public async Task<IActionResult> UnreadCount()
    {
        var userId = int.Parse(
            User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var count = await _notifService
            .GetUnreadCountAsync(userId);
        return Ok(new { count });
    }

    // PUT api/notifications/{id}/read
    [HttpPut("{id}/read")]
    public async Task<IActionResult> MarkRead(int id)
    {
        var userId = int.Parse(
            User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _notifService
            .MarkAsReadAsync(id, userId);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    // PUT api/notifications/read-all
    [HttpPut("read-all")]
    public async Task<IActionResult> MarkAllRead()
    {
        var userId = int.Parse(
            User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _notifService
            .MarkAllAsReadAsync(userId);
        return Ok(result);
    }
}