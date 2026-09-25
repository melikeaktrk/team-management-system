using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamTaskManager.Business.Services;

namespace TeamTaskManager.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationController : ControllerBase
{
    public NotificationController()
    {
    }

    [HttpGet]
    public IActionResult GetNotifications()
    {
        return Ok(new[]
        {
            new { id = Guid.NewGuid(), title = "Welcome", message = "Sisteme hoş geldiniz." }
        });
    }
}
