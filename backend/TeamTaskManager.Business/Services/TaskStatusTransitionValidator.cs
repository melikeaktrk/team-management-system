using TeamTaskManager.Entities;

namespace TeamTaskManager.Business.Services;

public static class TaskStatusTransitionValidator
{
    public static bool IsAllowed(TeamTaskManager.Entities.TaskStatus from, TeamTaskManager.Entities.TaskStatus to)
    {
        return (from, to) switch
        {
            (TeamTaskManager.Entities.TaskStatus.New, TeamTaskManager.Entities.TaskStatus.InProgress) => true,
            (TeamTaskManager.Entities.TaskStatus.New, TeamTaskManager.Entities.TaskStatus.Waiting) => true,
            (TeamTaskManager.Entities.TaskStatus.New, TeamTaskManager.Entities.TaskStatus.Completed) => true,
            (TeamTaskManager.Entities.TaskStatus.New, TeamTaskManager.Entities.TaskStatus.Cancelled) => true,
            (TeamTaskManager.Entities.TaskStatus.InProgress, TeamTaskManager.Entities.TaskStatus.Waiting) => true,
            (TeamTaskManager.Entities.TaskStatus.InProgress, TeamTaskManager.Entities.TaskStatus.Completed) => true,
            (TeamTaskManager.Entities.TaskStatus.InProgress, TeamTaskManager.Entities.TaskStatus.Cancelled) => true,
            (TeamTaskManager.Entities.TaskStatus.Waiting, TeamTaskManager.Entities.TaskStatus.InProgress) => true,
            (TeamTaskManager.Entities.TaskStatus.Waiting, TeamTaskManager.Entities.TaskStatus.Cancelled) => true,
            (TeamTaskManager.Entities.TaskStatus.Completed, TeamTaskManager.Entities.TaskStatus.InProgress) => true,
            (TeamTaskManager.Entities.TaskStatus.Cancelled, TeamTaskManager.Entities.TaskStatus.New) => true,
            _ => false
        };
    }

    public static (TeamTaskManager.Entities.TaskStatus Status, DateTime? CompletedDate)? ApplyCompletedDate(TeamTaskManager.Entities.TaskStatus currentStatus, TeamTaskManager.Entities.TaskStatus nextStatus, DateTime? currentCompletedDate)
    {
        if (!IsAllowed(currentStatus, nextStatus))
            return null;

        DateTime? completedDate = nextStatus == TeamTaskManager.Entities.TaskStatus.Completed ? DateTime.UtcNow : null;
        if (nextStatus != TeamTaskManager.Entities.TaskStatus.Completed && currentStatus == TeamTaskManager.Entities.TaskStatus.Completed)
            completedDate = null;

        return (nextStatus, completedDate);
    }
}
