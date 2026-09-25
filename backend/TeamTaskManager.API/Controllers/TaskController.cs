using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamTaskManager.Business.Services;
using TeamTaskManager.DTO.Task;

namespace TeamTaskManager.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TaskController : ControllerBase
{
    private readonly ITaskService _taskService;

    public TaskController(ITaskService taskService)
    {
        _taskService = taskService;
    }

    [HttpGet("project/{projectId:guid}")]
    public async Task<IActionResult> GetByProject(Guid projectId)
    {
        var tasks = await _taskService.GetByProjectAsync(projectId);
        return Ok(tasks);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var task = await _taskService.GetByIdAsync(id);
        return task is null ? NotFound() : Ok(task);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] TaskItemCreateRequest request)
    {
        var createdTask = await _taskService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = createdTask.Id }, createdTask);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] TaskItemUpdateRequest request)
    {
        var task = await _taskService.UpdateAsync(id, request);
        return task is null ? NotFound() : Ok(task);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _taskService.DeleteAsync(id);
        return deleted ? NoContent() : NotFound();
    }

    [HttpPost("{taskId:guid}/comments")]
    public async Task<IActionResult> AddComment(Guid taskId, [FromBody] TaskCommentCreateRequest request)
    {
        var userClaim = User.Claims.FirstOrDefault(c => c.Type.Contains("nameidentifier"));
        if (userClaim is null || !Guid.TryParse(userClaim.Value, out var userId))
            return Unauthorized();

        var comment = await _taskService.AddCommentAsync(taskId, userId, request);
        return Ok(comment);
    }
}
