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
    Task<bool> DeleteAsync(Guid id);
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
        var task = _mapper.Map<TaskItem>(request);
        await _unitOfWork.Tasks.AddAsync(task);
        await _unitOfWork.SaveChangesAsync();
        return _mapper.Map<TaskItemResponse>(task);
    }

    public async Task<TaskItemResponse?> UpdateAsync(Guid id, TaskItemUpdateRequest request)
    {
        var task = await _unitOfWork.Tasks.GetByIdAsync(id);
        if (task is null) return null;

        _mapper.Map(request, task);
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
