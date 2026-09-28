using AutoMapper;
using TeamTaskManager.DataAccess;
using TeamTaskManager.DTO.Project;
using TeamTaskManager.DTO.Task;
using TeamTaskManager.Entities;

namespace TeamTaskManager.Business.Services;

public interface ITaskService
{
    Task<IEnumerable<TaskItemResponse>> GetByProjectAsync(Guid projectId);
    async Task<PagedResponse<TaskItemResponse>> SearchByProjectAsync(Guid projectId, TaskSearchRequest request)
    {
        var items = (await GetByProjectAsync(projectId)).ToList();
        return new PagedResponse<TaskItemResponse> { Items = items, PageNumber = 1, PageSize = items.Count, TotalCount = items.Count, TotalPages = 1 };
    }
    Task<TaskItemResponse?> GetByIdAsync(Guid id);
    Task<TaskItemResponse> CreateAsync(TaskItemCreateRequest request);
    Task<TaskItemResponse> CreateAsync(TaskItemCreateRequest request, Guid? actorUserId) => CreateAsync(request);
    Task<TaskItemResponse?> UpdateAsync(Guid id, TaskItemUpdateRequest request);
    Task<TaskItemResponse?> UpdateAsync(Guid id, TaskItemUpdateRequest request, Guid? actorUserId) => UpdateAsync(id, request);
    Task<TaskItemResponse?> UpdateStatusAsync(Guid id, TeamTaskManager.Entities.TaskStatus status);
    Task<TaskItemResponse?> UpdateStatusAsync(Guid id, TeamTaskManager.Entities.TaskStatus status, Guid? actorUserId) => UpdateStatusAsync(id, status);
    Task<bool> DeleteAsync(Guid id);
    Task<bool> DeleteAsync(Guid id, Guid? actorUserId) => DeleteAsync(id);
    Task<IEnumerable<TaskCommentResponse>> GetCommentsAsync(Guid taskId);
    Task<TaskCommentResponse> AddCommentAsync(Guid taskId, Guid userId, TaskCommentCreateRequest request);
    Task<IEnumerable<TaskActivityResponse>> GetActivityForTaskAsync(Guid taskId) => Task.FromResult<IEnumerable<TaskActivityResponse>>(Array.Empty<TaskActivityResponse>());
    Task<ProjectReportResponse?> GetProjectReportAsync(Guid projectId) => Task.FromResult<ProjectReportResponse?>(null);
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
        var tasks = (await _unitOfWork.Tasks.GetAllAsync()).Where(task => task.ProjectId == projectId).ToList();
        return _mapper.Map<IEnumerable<TaskItemResponse>>(tasks);
    }

    public async Task<PagedResponse<TaskItemResponse>> SearchByProjectAsync(Guid projectId, TaskSearchRequest request)
    {
        IEnumerable<TaskItem> query = (await _unitOfWork.Tasks.GetAllAsync()).Where(task => task.ProjectId == projectId);
        if (!string.IsNullOrWhiteSpace(request.Status))
            query = query.Where(task => task.Status.ToString().Equals(request.Status, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(request.Priority))
            query = query.Where(task => task.Priority.ToString().Equals(request.Priority, StringComparison.OrdinalIgnoreCase));
        if (request.AssignedToUserId.HasValue)
            query = query.Where(task => task.AssignedToUserId == request.AssignedToUserId);
        if (request.DueFrom.HasValue)
            query = query.Where(task => task.DueDate.HasValue && task.DueDate.Value.Date >= request.DueFrom.Value.Date);
        if (request.DueTo.HasValue)
            query = query.Where(task => task.DueDate.HasValue && task.DueDate.Value.Date <= request.DueTo.Value.Date);

        var descending = string.Equals(request.SortDirection, "desc", StringComparison.OrdinalIgnoreCase);
        query = request.SortBy?.ToLowerInvariant() switch
        {
            "title" => descending ? query.OrderByDescending(task => task.Title) : query.OrderBy(task => task.Title),
            "status" => descending ? query.OrderByDescending(task => task.Status) : query.OrderBy(task => task.Status),
            "priority" => descending ? query.OrderByDescending(task => task.Priority) : query.OrderBy(task => task.Priority),
            "createdat" => descending ? query.OrderByDescending(task => task.CreatedAt) : query.OrderBy(task => task.CreatedAt),
            _ => descending ? query.OrderByDescending(task => task.DueDate) : query.OrderBy(task => task.DueDate)
        };

        var totalCount = query.Count();
        var items = query.Skip((request.PageNumber - 1) * request.PageSize).Take(request.PageSize).ToList();
        return new PagedResponse<TaskItemResponse>
        {
            Items = _mapper.Map<IReadOnlyList<TaskItemResponse>>(items),
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize)
        };
    }

    public async Task<TaskItemResponse?> GetByIdAsync(Guid id)
    {
        var task = await _unitOfWork.Tasks.GetByIdAsync(id);
        return task is null ? null : _mapper.Map<TaskItemResponse>(task);
    }

    public async Task<TaskItemResponse> CreateAsync(TaskItemCreateRequest request, Guid? actorUserId)
    {
        if (!Enum.TryParse<TaskPriority>(request.Priority, true, out var priority) || !Enum.IsDefined(priority))
            throw new InvalidOperationException("Geçersiz görev önceliği.");

        var project = await _unitOfWork.Projects.GetByIdAsync(request.ProjectId);
        if (project is null) throw new InvalidOperationException("Proje bulunamadı.");
        if (project.ProjectStatus is ProjectStatus.Completed or ProjectStatus.Cancelled)
            throw new InvalidOperationException("Tamamlanmış veya iptal edilmiş projeye görev eklenemez.");
        if (request.DueDate.HasValue && project.StartDate.HasValue && request.DueDate.Value.Date < project.StartDate.Value.Date)
            throw new InvalidOperationException("Görev bitiş tarihi proje başlangıç tarihinden önce olamaz.");
        await ValidateAssigneeAsync(request.ProjectId, request.AssignedToUserId);

        var task = _mapper.Map<TaskItem>(request);
        task.Status = TeamTaskManager.Entities.TaskStatus.New;
        task.Priority = priority;
        task.CreatedByUserId = actorUserId;
        await _unitOfWork.Tasks.AddAsync(task);
        await AddActivityAsync(actorUserId, task.Id, "TaskCreated", $"'{task.Title}' görevi oluşturuldu.");
        if (task.AssignedToUserId.HasValue)
            await AddNotificationAsync(task.AssignedToUserId.Value, "Yeni görev atandı", $"'{task.Title}' görevi size atandı.", task.Id);
        await _unitOfWork.SaveChangesAsync();
        return _mapper.Map<TaskItemResponse>(task);
    }

    public Task<TaskItemResponse> CreateAsync(TaskItemCreateRequest request) => CreateAsync(request, null);

    public async Task<TaskItemResponse?> UpdateAsync(Guid id, TaskItemUpdateRequest request, Guid? actorUserId)
    {
        var task = await _unitOfWork.Tasks.GetByIdAsync(id);
        if (task is null) return null;
        var previousStatus = task.Status;
        var previousAssignee = task.AssignedToUserId;
        var taskFieldsChanged =
            (request.Title is not null && request.Title != task.Title) ||
            (request.Description is not null && request.Description != task.Description) ||
            (request.Priority is not null && !request.Priority.Equals(task.Priority.ToString(), StringComparison.OrdinalIgnoreCase)) ||
            (request.DueDate.HasValue && request.DueDate != task.DueDate);

        (TeamTaskManager.Entities.TaskStatus Status, DateTime? CompletedDate)? statusChange = null;
        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            if (!Enum.TryParse<TeamTaskManager.Entities.TaskStatus>(request.Status, true, out var nextStatus) || !Enum.IsDefined(nextStatus))
                throw new InvalidOperationException("Geçersiz görev durumu.");
            statusChange = nextStatus == task.Status
                ? (task.Status, task.CompletedDate)
                : TaskStatusTransitionValidator.ApplyCompletedDate(task.Status, nextStatus, task.CompletedDate);
            if (statusChange is null) throw new InvalidOperationException("Görev durumu geçişine izin verilmiyor.");
        }

        if (!string.IsNullOrWhiteSpace(request.Priority) &&
            (!Enum.TryParse<TaskPriority>(request.Priority, true, out var nextPriority) || !Enum.IsDefined(nextPriority)))
            throw new InvalidOperationException("Geçersiz görev önceliği.");

        if (request.AssignedToUserId.HasValue && request.AssignedToUserId != task.AssignedToUserId)
            await ValidateAssigneeAsync(task.ProjectId, request.AssignedToUserId);
        if (request.DueDate.HasValue)
        {
            var project = await _unitOfWork.Projects.GetByIdAsync(task.ProjectId);
            if (project?.StartDate.HasValue == true && request.DueDate.Value.Date < project.StartDate.Value.Date)
                throw new InvalidOperationException("Görev bitiş tarihi proje başlangıç tarihinden önce olamaz.");
        }

        _mapper.Map(request, task);
        if (request.ClearAssignment)
            task.AssignedToUserId = null;
        if (statusChange is not null)
        {
            task.Status = statusChange.Value.Status;
            task.CompletedDate = statusChange.Value.CompletedDate;
        }
        task.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Tasks.Update(task);
        if (previousStatus != task.Status)
        {
            await AddActivityAsync(actorUserId, task.Id, "TaskStatusChanged", $"'{task.Title}' durumu {previousStatus} → {task.Status} olarak değiştirildi.");
            await NotifyTaskStatusChangedAsync(task, actorUserId);
        }
        if (previousAssignee != task.AssignedToUserId && task.AssignedToUserId.HasValue)
        {
            await AddActivityAsync(actorUserId, task.Id, "TaskAssigned", $"'{task.Title}' görevi yeni bir üyeye atandı.");
            await AddNotificationAsync(task.AssignedToUserId.Value, "Yeni görev atandı", $"'{task.Title}' görevi size atandı.", task.Id);
        }
        if (previousAssignee != task.AssignedToUserId && task.AssignedToUserId is null)
            await AddActivityAsync(actorUserId, task.Id, "TaskUnassigned", $"'{task.Title}' görevinin ataması kaldırıldı.");
        if (taskFieldsChanged)
            await AddActivityAsync(actorUserId, task.Id, "TaskUpdated", $"'{task.Title}' görevinin bilgileri güncellendi.");
        await _unitOfWork.SaveChangesAsync();
        return _mapper.Map<TaskItemResponse>(task);
    }

    public Task<TaskItemResponse?> UpdateAsync(Guid id, TaskItemUpdateRequest request) => UpdateAsync(id, request, null);

    public async Task<TaskItemResponse?> UpdateStatusAsync(Guid id, TeamTaskManager.Entities.TaskStatus status, Guid? actorUserId)
    {
        var task = await _unitOfWork.Tasks.GetByIdAsync(id);
        if (task is null) return null;
        var previousStatus = task.Status;
        var statusChange = TaskStatusTransitionValidator.ApplyCompletedDate(task.Status, status, task.CompletedDate);
        if (statusChange is null) throw new InvalidOperationException("Görev durumu geçişine izin verilmiyor.");
        task.Status = statusChange.Value.Status;
        task.CompletedDate = statusChange.Value.CompletedDate;
        task.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Tasks.Update(task);
        if (previousStatus != task.Status)
        {
            await AddActivityAsync(actorUserId, task.Id, "TaskStatusChanged", $"'{task.Title}' durumu {previousStatus} → {task.Status} olarak değiştirildi.");
            await NotifyTaskStatusChangedAsync(task, actorUserId);
        }
        await _unitOfWork.SaveChangesAsync();
        return _mapper.Map<TaskItemResponse>(task);
    }

    public Task<TaskItemResponse?> UpdateStatusAsync(Guid id, TeamTaskManager.Entities.TaskStatus status) => UpdateStatusAsync(id, status, null);

    public async Task<bool> DeleteAsync(Guid id, Guid? actorUserId)
    {
        var task = await _unitOfWork.Tasks.GetByIdAsync(id);
        if (task is null) return false;
        await AddActivityAsync(actorUserId, task.Id, "TaskDeleted", $"'{task.Title}' görevi silindi.");
        _unitOfWork.Tasks.Delete(task);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    public Task<bool> DeleteAsync(Guid id) => DeleteAsync(id, null);

    public async Task<IEnumerable<TaskCommentResponse>> GetCommentsAsync(Guid taskId)
    {
        var comments = (await _unitOfWork.TaskComments.GetAllAsync()).Where(comment => comment.TaskItemId == taskId).OrderBy(comment => comment.CreatedAt).ToList();
        return _mapper.Map<IEnumerable<TaskCommentResponse>>(comments);
    }

    public async Task<TaskCommentResponse> AddCommentAsync(Guid taskId, Guid userId, TaskCommentCreateRequest request)
    {
        var task = await _unitOfWork.Tasks.GetByIdAsync(taskId);
        if (task is null) throw new InvalidOperationException("Görev bulunamadı.");
        var comment = _mapper.Map<TaskComment>(request);
        comment.TaskItemId = taskId;
        comment.UserId = userId;
        await _unitOfWork.TaskComments.AddAsync(comment);
        await AddActivityAsync(userId, task.Id, "TaskCommentCreated", $"{task.Title} için yorum eklendi.");
        await _unitOfWork.SaveChangesAsync();
        return _mapper.Map<TaskCommentResponse>(comment);
    }

    public async Task<IEnumerable<TaskActivityResponse>> GetActivityForTaskAsync(Guid taskId)
    {
        var logs = (await _unitOfWork.ActivityLogs.GetAllAsync())
            .Where(log => log.EntityType == nameof(TaskItem) && log.EntityId == taskId)
            .OrderByDescending(log => log.CreatedAt).ToList();
        var users = (await _unitOfWork.Users.GetAllAsync()).ToDictionary(user => user.Id);
        return logs.Select(log => ToActivityResponse(log, users));
    }

    public async Task<ProjectReportResponse?> GetProjectReportAsync(Guid projectId)
    {
        var project = await _unitOfWork.Projects.GetByIdAsync(projectId);
        if (project is null) return null;
        var tasks = (await _unitOfWork.Tasks.GetAllAsync()).Where(task => task.ProjectId == projectId).ToList();
        var taskIds = tasks.Select(task => task.Id).ToHashSet();
        var users = (await _unitOfWork.Users.GetAllAsync()).ToDictionary(user => user.Id);
        var logs = (await _unitOfWork.ActivityLogs.GetAllAsync())
            .Where(log => log.EntityType == nameof(TaskItem) && log.EntityId.HasValue && taskIds.Contains(log.EntityId.Value))
            .OrderByDescending(log => log.CreatedAt).Take(10).ToList();
        var today = DateTime.UtcNow.Date;

        return new ProjectReportResponse
        {
            ProjectId = project.Id,
            ProjectName = project.Name,
            TotalTasks = tasks.Count,
            CompletedTasks = tasks.Count(task => task.Status == TeamTaskManager.Entities.TaskStatus.Completed),
            InProgressTasks = tasks.Count(task => task.Status == TeamTaskManager.Entities.TaskStatus.InProgress),
            OverdueTasks = tasks.Count(task => task.DueDate.HasValue && task.DueDate.Value.Date < today && task.Status is not (TeamTaskManager.Entities.TaskStatus.Completed or TeamTaskManager.Entities.TaskStatus.Cancelled)),
            CompletionPercentage = tasks.Count == 0 ? 0 : Math.Round(tasks.Count(task => task.Status == TeamTaskManager.Entities.TaskStatus.Completed) * 100m / tasks.Count, 1),
            MemberTaskDistribution = tasks.Where(task => task.AssignedToUserId.HasValue)
                .GroupBy(task => task.AssignedToUserId!.Value)
                .Select(group => new MemberTaskSummary
                {
                    UserId = group.Key,
                    UserName = users.TryGetValue(group.Key, out var user) ? user.UserName ?? "Kullanıcı" : "Silinmiş kullanıcı",
                    OpenTasks = group.Count(task => task.Status is not (TeamTaskManager.Entities.TaskStatus.Completed or TeamTaskManager.Entities.TaskStatus.Cancelled)),
                    CompletedTasks = group.Count(task => task.Status == TeamTaskManager.Entities.TaskStatus.Completed)
                }).OrderByDescending(member => member.OpenTasks).ToList(),
            RecentActivities = logs.Select(log => ToActivityResponse(log, users)).ToList()
        };
    }

    private async Task ValidateAssigneeAsync(Guid projectId, Guid? assigneeId)
    {
        if (!assigneeId.HasValue) return;
        var user = await _unitOfWork.Users.GetByIdAsync(assigneeId.Value);
        if (user is null || !user.IsActive)
            throw new InvalidOperationException("Görev yalnızca aktif bir proje üyesine atanabilir.");
        var members = await _unitOfWork.ProjectMembers.GetAllAsync();
        if (!members.Any(member => member.ProjectId == projectId && member.UserId == assigneeId.Value && member.IsActive))
            throw new InvalidOperationException("Görev yalnızca ilgili projenin aktif üyelerinden birine atanabilir.");
    }

    private async Task AddActivityAsync(Guid? actorUserId, Guid taskId, string action, string description)
    {
        await _unitOfWork.ActivityLogs.AddAsync(new ActivityLog
        {
            UserId = actorUserId,
            EntityType = nameof(TaskItem),
            EntityId = taskId,
            Action = action,
            Description = description
        });
    }

    private async Task AddNotificationAsync(Guid userId, string title, string message, Guid taskId)
    {
        await _unitOfWork.Notifications.AddAsync(new Notification
        {
            UserId = userId,
            Title = title,
            Message = message,
            Type = NotificationType.Task,
            RelatedEntityId = taskId
        });
    }

    private async Task NotifyTaskStatusChangedAsync(TaskItem task, Guid? actorUserId)
    {
        var project = await _unitOfWork.Projects.GetByIdAsync(task.ProjectId);
        var recipients = new HashSet<Guid>();
        if (project?.ManagerUserId is Guid managerId && managerId != actorUserId)
            recipients.Add(managerId);
        if (task.AssignedToUserId is Guid assigneeId && assigneeId != actorUserId)
            recipients.Add(assigneeId);
        foreach (var recipientId in recipients)
            await AddNotificationAsync(recipientId, "Görev durumu güncellendi", $"'{task.Title}' görevinin durumu {task.Status} oldu.", task.Id);
    }

    private static TaskActivityResponse ToActivityResponse(ActivityLog log, IReadOnlyDictionary<Guid, ApplicationUser> users) => new()
    {
        Id = log.Id,
        UserId = log.UserId,
        UserName = log.UserId.HasValue && users.TryGetValue(log.UserId.Value, out var user) ? user.UserName : null,
        Action = log.Action,
        EntityType = log.EntityType,
        Description = log.Description,
        CreatedAt = log.CreatedAt
    };
}
