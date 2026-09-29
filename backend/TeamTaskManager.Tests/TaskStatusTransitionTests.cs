using TeamTaskManager.Business.Services;
using TeamTaskManager.Entities;

namespace TeamTaskManager.Tests;

// Durum geçiş tablosunun izinli/engelli sonuçlarını ve bitiş tarihini test eder.
public class TaskStatusTransitionTests
{
    // İş kuralında izin verilen farklı geçiş örnekleri true üretmelidir.
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

    // Tanımlı olmayan geçişler false olmalıdır.
    [Theory]
    [InlineData(TeamTaskManager.Entities.TaskStatus.InProgress, TeamTaskManager.Entities.TaskStatus.New)]
    [InlineData(TeamTaskManager.Entities.TaskStatus.Waiting, TeamTaskManager.Entities.TaskStatus.Completed)]
    [InlineData(TeamTaskManager.Entities.TaskStatus.Cancelled, TeamTaskManager.Entities.TaskStatus.Completed)]
    public void DisallowedTransitions_ShouldReturnFalse(TeamTaskManager.Entities.TaskStatus from, TeamTaskManager.Entities.TaskStatus to)
    {
        var isAllowed = TaskStatusTransitionValidator.IsAllowed(from, to);

        Assert.False(isAllowed);
    }

    // Completed durumuna geçildiğinde CompletedDate atanmasını doğrular.
    [Fact]
    public void CompletingTask_ShouldSetCompletedDate()
    {
        var completed = TaskStatusTransitionValidator.ApplyCompletedDate(TeamTaskManager.Entities.TaskStatus.New, TeamTaskManager.Entities.TaskStatus.Completed, null);

        Assert.NotNull(completed);
        Assert.Equal(TeamTaskManager.Entities.TaskStatus.Completed, completed.Value.Status);
        Assert.NotNull(completed.Value.CompletedDate);
    }
}
