using TeamTaskManager.Business.Services;
using TeamTaskManager.Entities;

namespace TeamTaskManager.Tests;

public class TaskStatusTransitionTests
{
    [Theory]
    [InlineData(TeamTaskManager.Entities.TaskStatus.New, TeamTaskManager.Entities.TaskStatus.InProgress)]
    [InlineData(TeamTaskManager.Entities.TaskStatus.New, TeamTaskManager.Entities.TaskStatus.Waiting)]
    [InlineData(TeamTaskManager.Entities.TaskStatus.InProgress, TeamTaskManager.Entities.TaskStatus.Completed)]
    [InlineData(TeamTaskManager.Entities.TaskStatus.Waiting, TeamTaskManager.Entities.TaskStatus.InProgress)]
    [InlineData(TeamTaskManager.Entities.TaskStatus.Completed, TeamTaskManager.Entities.TaskStatus.InProgress)]
    public void AllowedTransitions_ShouldReturnTrue(TeamTaskManager.Entities.TaskStatus from, TeamTaskManager.Entities.TaskStatus to)
    {
        var isAllowed = TaskStatusTransitionValidator.IsAllowed(from, to);

        Assert.True(isAllowed);
    }

    [Theory]
    [InlineData(TeamTaskManager.Entities.TaskStatus.InProgress, TeamTaskManager.Entities.TaskStatus.New)]
    [InlineData(TeamTaskManager.Entities.TaskStatus.Waiting, TeamTaskManager.Entities.TaskStatus.Completed)]
    [InlineData(TeamTaskManager.Entities.TaskStatus.Cancelled, TeamTaskManager.Entities.TaskStatus.Completed)]
    public void DisallowedTransitions_ShouldReturnFalse(TeamTaskManager.Entities.TaskStatus from, TeamTaskManager.Entities.TaskStatus to)
    {
        var isAllowed = TaskStatusTransitionValidator.IsAllowed(from, to);

        Assert.False(isAllowed);
    }

    [Fact]
    public void CompletingTask_ShouldSetCompletedDate()
    {
        var completed = TaskStatusTransitionValidator.ApplyCompletedDate(TeamTaskManager.Entities.TaskStatus.New, TeamTaskManager.Entities.TaskStatus.Completed, null);

        Assert.NotNull(completed);
        Assert.Equal(TeamTaskManager.Entities.TaskStatus.Completed, completed.Value.Status);
        Assert.NotNull(completed.Value.CompletedDate);
    }
}
