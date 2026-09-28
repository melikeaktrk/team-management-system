using AutoMapper;
using TeamTaskManager.DataAccess;
using TeamTaskManager.DTO.Task;
using TeamTaskManager.Entities;

namespace TeamTaskManager.Business.Services;

public interface ITaskService
{
    Task<IEnumerable<TaskItemResponse>> GetByProjectAsync(Guid projectId);
    Task<TaskItemResponse?> GetByIdAsync(Guid id);
    Task<TaskItemResponse> CreateAsync(TaskItemCreateRequest request);
    Task<TaskItemResponse?> UpdateAsync(Guid id, TaskItemUpdateRequest request);
    Task<TaskItemResponse?> UpdateStatusAsync(Guid id, TeamTaskManager.Entities.TaskStatus status);
    Task<bool> DeleteAsync(Guid id);
    Task<IEnumerable<TaskCommentResponse>> GetCommentsAsync(Guid taskId);
    Task<TaskCommentResponse> AddCommentAsync(Guid taskId, Guid userId, TaskCommentCreateRequest request);
}

public class TaskService : ITaskService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public TaskService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<IEnumerable<TaskItemResponse>> GetByProjectAsync(Guid projectId)
    {
        var tasks = (await _unitOfWork.Tasks.GetAllAsync())
            .Where(x => x.ProjectId == projectId)
            .ToList();

        return _mapper.Map<IEnumerable<TaskItemResponse>>(tasks);
    }

    public async Task<TaskItemResponse?> GetByIdAsync(Guid id)
    {
        var task = await _unitOfWork.Tasks.GetByIdAsync(id);
        return task is null ? null : _mapper.Map<TaskItemResponse>(task);
    }

    public async Task<TaskItemResponse> CreateAsync(TaskItemCreateRequest request)
    {
        if (!Enum.TryParse<TaskPriority>(request.Priority, true, out var priority) ||
            !Enum.IsDefined(priority))
        {
            throw new InvalidOperationException("Geçersiz görev önceliği.");
        }

        var task = _mapper.Map<TaskItem>(request);
        task.Status = TeamTaskManager.Entities.TaskStatus.New;
        task.Priority = priority;
        await _unitOfWork.Tasks.AddAsync(task);
        await _unitOfWork.SaveChangesAsync();
        return _mapper.Map<TaskItemResponse>(task);
    }

    public async Task<TaskItemResponse?> UpdateAsync(Guid id, TaskItemUpdateRequest request)
    {
        var task = await _unitOfWork.Tasks.GetByIdAsync(id);
        if (task is null) return null;

        (TeamTaskManager.Entities.TaskStatus Status, DateTime? CompletedDate)? statusChange = null;
        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            if (!Enum.TryParse<TeamTaskManager.Entities.TaskStatus>(request.Status, true, out var nextStatus) ||
                !Enum.IsDefined(nextStatus))
            {
                throw new InvalidOperationException("Geçersiz görev durumu.");
            }

            statusChange = nextStatus == task.Status
                ? (task.Status, task.CompletedDate)
                : TaskStatusTransitionValidator.ApplyCompletedDate(
                    task.Status, nextStatus, task.CompletedDate);
            if (statusChange is null)
            {
                throw new InvalidOperationException("Görev durumu geçişine izin verilmiyor.");
            }
        }

        if (!string.IsNullOrWhiteSpace(request.Priority) &&
            (!Enum.TryParse<TaskPriority>(request.Priority, true, out var nextPriority) ||
             !Enum.IsDefined(nextPriority)))
        {
            throw new InvalidOperationException("Geçersiz görev önceliği.");
        }

        _mapper.Map(request, task);
        if (statusChange is not null)
        {
            task.Status = statusChange.Value.Status;
            task.CompletedDate = statusChange.Value.CompletedDate;
        }
        task.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Tasks.Update(task);
        await _unitOfWork.SaveChangesAsync();
        return _mapper.Map<TaskItemResponse>(task);
    }

    public async Task<TaskItemResponse?> UpdateStatusAsync(Guid id, TeamTaskManager.Entities.TaskStatus status)
    {
        var task = await _unitOfWork.Tasks.GetByIdAsync(id);
        if (task is null) return null;

        var statusChange = TaskStatusTransitionValidator.ApplyCompletedDate(
            task.Status, status, task.CompletedDate);
        if (statusChange is null)
            throw new InvalidOperationException("Görev durumu geçişine izin verilmiyor.");

        task.Status = statusChange.Value.Status;
        task.CompletedDate = statusChange.Value.CompletedDate;
        task.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Tasks.Update(task);
        await _unitOfWork.SaveChangesAsync();
        return _mapper.Map<TaskItemResponse>(task);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var task = await _unitOfWork.Tasks.GetByIdAsync(id);
        if (task is null) return false;

        _unitOfWork.Tasks.Delete(task);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<TaskCommentResponse>> GetCommentsAsync(Guid taskId)
    {
        var comments = (await _unitOfWork.TaskComments.GetAllAsync())
            .Where(comment => comment.TaskItemId == taskId)
            .OrderBy(comment => comment.CreatedAt)
            .ToList();

        return _mapper.Map<IEnumerable<TaskCommentResponse>>(comments);
    }

    public async Task<TaskCommentResponse> AddCommentAsync(Guid taskId, Guid userId, TaskCommentCreateRequest request)
    {
        var task = await _unitOfWork.Tasks.GetByIdAsync(taskId);
        if (task is null)
            throw new InvalidOperationException("Görev bulunamadı.");

        var comment = _mapper.Map<TaskComment>(request);
        comment.TaskItemId = taskId;
        comment.UserId = userId;

        await _unitOfWork.TaskComments.AddAsync(comment);

        await _unitOfWork.ActivityLogs.AddAsync(new ActivityLog
        {
            UserId = userId,
            Action = "TaskCommentCreated",
            EntityType = nameof(TaskComment),
            EntityId = comment.Id,
            Description = $"{task.Title} için yorum eklendi."
        });

        await _unitOfWork.SaveChangesAsync();
        return _mapper.Map<TaskCommentResponse>(comment);
    }
}
