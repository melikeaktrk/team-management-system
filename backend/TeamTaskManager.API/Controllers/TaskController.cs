using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TeamTaskManager.Business.Services;
using TeamTaskManager.DTO.Task;

namespace TeamTaskManager.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TaskController : ControllerBase
{
    private readonly ITaskService _taskService;
    private readonly IProjectService _projectService;
    private readonly ITaskAttachmentService _attachmentService;

    public TaskController(
        ITaskService taskService,
        IProjectService projectService,
        ITaskAttachmentService attachmentService)
    {
        _taskService = taskService;
        _projectService = projectService;
        _attachmentService = attachmentService;
    }

    [HttpGet("project/{projectId:guid}")]
    public async Task<IActionResult> GetByProject(Guid projectId)
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();

        if (User.IsInRole("Admin"))
            return Ok(await _taskService.GetByProjectAsync(projectId));

        if (User.IsInRole("ProjectManager"))
        {
            if (!await _projectService.IsManagerAsync(projectId, userId.Value))
                return Forbid();

            return Ok(await _taskService.GetByProjectAsync(projectId));
        }

        if (!User.IsInRole("TeamMember")) return Forbid();

        var assignedTasks = await _taskService.GetByProjectAsync(projectId);
        return Ok(assignedTasks.Where(task => task.AssignedToUserId == userId.Value));
    }

    [HttpGet("project/{projectId:guid}/search")]
    public async Task<IActionResult> SearchByProject(Guid projectId, [FromQuery] TaskSearchRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();
        if (request.PageNumber < 1 || request.PageSize < 1 || request.PageSize > 100)
            return BadRequest(new { message = "Sayfa numarası en az 1, sayfa boyutu 1-100 arasında olmalıdır." });
        if (request.DueFrom.HasValue && request.DueTo.HasValue && request.DueFrom > request.DueTo)
            return BadRequest(new { message = "Başlangıç tarihi bitiş tarihinden sonra olamaz." });

        if (!User.IsInRole("Admin"))
        {
            if (User.IsInRole("ProjectManager"))
            {
                if (!await _projectService.IsManagerAsync(projectId, userId.Value)) return Forbid();
            }
            else if (!User.IsInRole("TeamMember")) return Forbid();
        }

        if (User.IsInRole("TeamMember"))
            request.AssignedToUserId = userId.Value;
        var result = await _taskService.SearchByProjectAsync(projectId, request);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var task = await _taskService.GetByIdAsync(id);
        if (task is null) return NotFound();

        return await CanAccessTaskAsync(task)
            ? Ok(task)
            : Forbid();
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] TaskItemCreateRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();

        if (!User.IsInRole("Admin"))
        {
            if (!User.IsInRole("ProjectManager") ||
                !await _projectService.IsManagerAsync(request.ProjectId, userId.Value))
            {
                return Forbid();
            }
        }

        try
        {
            var createdTask = await _taskService.CreateAsync(request, userId.Value);
            return CreatedAtAction(nameof(GetById), new { id = createdTask.Id }, createdTask);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] TaskItemUpdateRequest request)
    {
        var task = await _taskService.GetByIdAsync(id);
        if (task is null) return NotFound();

        if (!User.IsInRole("Admin"))
        {
            var userId = GetCurrentUserId();
            if (userId is null) return Unauthorized();

            if (User.IsInRole("ProjectManager"))
            {
                if (!await _projectService.IsManagerAsync(task.ProjectId, userId.Value))
                    return Forbid();
            }
            else if (User.IsInRole("TeamMember"))
            {
                if (task.AssignedToUserId != userId.Value) return Forbid();
                if (request.Title is not null || request.Description is not null ||
                    request.Priority is not null || request.DueDate is not null ||
                    request.AssignedToUserId is not null || request.ClearAssignment)
                {
                    return BadRequest(new { message = "TeamMember yalnızca görev durumunu değiştirebilir." });
                }

                if (string.IsNullOrWhiteSpace(request.Status) ||
                    !Enum.TryParse<TeamTaskManager.Entities.TaskStatus>(request.Status, true, out var status) ||
                    !Enum.IsDefined(status))
                {
                    return BadRequest(new { message = "Geçersiz görev durumu." });
                }

                try
                {
                    var statusUpdate = await _taskService.UpdateStatusAsync(id, status, userId.Value);
                    return statusUpdate is null ? NotFound() : Ok(statusUpdate);
                }
                catch (InvalidOperationException ex)
                {
                    return BadRequest(new { message = ex.Message });
                }
            }
            else
            {
                return Forbid();
            }
        }

        try
        {
            var updatedTask = await _taskService.UpdateAsync(id, request, GetCurrentUserId());
            return updatedTask is null ? NotFound() : Ok(updatedTask);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var task = await _taskService.GetByIdAsync(id);
        if (task is null) return NotFound();

        if (!User.IsInRole("Admin"))
        {
            var userId = GetCurrentUserId();
            if (userId is null) return Unauthorized();
            if (!User.IsInRole("ProjectManager") ||
                !await _projectService.IsManagerAsync(task.ProjectId, userId.Value))
            {
                return Forbid();
            }
        }

        var deleted = await _taskService.DeleteAsync(id, GetCurrentUserId());
        return deleted ? NoContent() : NotFound();
    }

    [HttpPost("{taskId:guid}/comments")]
    public async Task<IActionResult> AddComment(Guid taskId, [FromBody] TaskCommentCreateRequest request)
    {
        var task = await _taskService.GetByIdAsync(taskId);
        if (task is null) return NotFound();
        if (!await CanAccessTaskAsync(task)) return Forbid();

        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();

        var comment = await _taskService.AddCommentAsync(taskId, userId.Value, request);
        return Ok(comment);
    }

    [HttpGet("{taskId:guid}/comments")]
    public async Task<IActionResult> GetComments(Guid taskId)
    {
        var task = await _taskService.GetByIdAsync(taskId);
        if (task is null) return NotFound();
        if (!await CanAccessTaskAsync(task)) return Forbid();

        return Ok(await _taskService.GetCommentsAsync(taskId));
    }

    [HttpGet("{taskId:guid}/activity")]
    public async Task<IActionResult> GetActivity(Guid taskId)
    {
        var task = await _taskService.GetByIdAsync(taskId);
        if (task is null) return NotFound();
        if (!await CanAccessTaskAsync(task)) return Forbid();
        return Ok(await _taskService.GetActivityForTaskAsync(taskId));
    }

    [HttpGet("{taskId:guid}/attachments")]
    public async Task<IActionResult> GetAttachments(Guid taskId)
    {
        var task = await _taskService.GetByIdAsync(taskId);
        if (task is null) return NotFound();
        if (!await CanAccessTaskAsync(task)) return Forbid();

        return Ok(await _attachmentService.GetByTaskAsync(taskId));
    }

    [HttpPost("{taskId:guid}/attachments")]
    [RequestSizeLimit(10 * 1024 * 1024 + 65_536)]
    public async Task<IActionResult> UploadAttachment(Guid taskId, [FromForm] IFormFile file)
    {
        var task = await _taskService.GetByIdAsync(taskId);
        if (task is null) return NotFound();
        if (!await CanAccessTaskAsync(task)) return Forbid();

        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();

        try
        {
            var attachment = await _attachmentService.UploadAsync(taskId, userId.Value, file);
            return Ok(attachment);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("attachments/{attachmentId:guid}")]
    public async Task<IActionResult> DownloadAttachment(Guid attachmentId)
    {
        var metadata = await _attachmentService.GetByIdAsync(attachmentId);
        if (metadata is null) return NotFound();

        var task = await _taskService.GetByIdAsync(metadata.TaskItemId);
        if (task is null) return NotFound();
        if (!await CanAccessTaskAsync(task)) return Forbid();

        var download = await _attachmentService.DownloadAsync(attachmentId);
        return download is null
            ? NotFound()
            : File(download.Value.Content, download.Value.ContentType, download.Value.FileName);
    }

    private async Task<bool> CanAccessTaskAsync(TaskItemResponse task)
    {
        if (User.IsInRole("Admin")) return true;

        var userId = GetCurrentUserId();
        if (userId is null) return false;

        if (User.IsInRole("ProjectManager"))
            return await _projectService.IsManagerAsync(task.ProjectId, userId.Value);

        return User.IsInRole("TeamMember") && task.AssignedToUserId == userId.Value;
    }

    private Guid? GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("nameid");
        return Guid.TryParse(value, out var userId) ? userId : null;
    }
}
