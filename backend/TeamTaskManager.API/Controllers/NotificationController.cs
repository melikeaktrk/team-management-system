using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TeamTaskManager.Business.Services;

namespace TeamTaskManager.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
// Bildirim uçları yalnızca giriş yapmış kullanıcının kendi kayıtları üzerinde çalışır.
public class NotificationController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public NotificationController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    // Oturum sahibinin bildirimlerini tarih sırasıyla listeler.
    [HttpGet]
    public async Task<IActionResult> GetNotifications()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("nameid");
        if (!Guid.TryParse(claim, out var userId)) return Unauthorized();

        return Ok(await _notificationService.GetForUserAsync(userId));
    }

    // Tek bildirimi okundu yapar; servis başka kullanıcının bildirimini bulamaz.
    [HttpPatch("{id:guid}/read")]
    public async Task<IActionResult> MarkAsRead(Guid id)
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();

        var notification = await _notificationService.MarkAsReadAsync(id, userId.Value);
        return notification is null ? NotFound() : Ok(notification);
    }

    // Oturum sahibinin tüm okunmamış bildirimlerini okundu işaretler.
    [HttpPatch("read-all")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();

        var updatedCount = await _notificationService.MarkAllAsReadAsync(userId.Value);
        return Ok(new { updatedCount });
    }

    // JWT claim'lerinden GUID kullanıcı kimliğini güvenli biçimde çıkarır.
    private Guid? GetCurrentUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("nameid");
        return Guid.TryParse(claim, out var userId) ? userId : null;
    }
}
