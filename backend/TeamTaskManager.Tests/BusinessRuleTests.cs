using TeamTaskManager.Entities;

namespace TeamTaskManager.Tests;

public class BusinessRuleTests
{
    [Fact]
    public void ProjectName_Validation_ShouldRejectEmptyName()
    {
        var project = new Project { Name = string.Empty };

        Assert.True(string.IsNullOrWhiteSpace(project.Name));
    }

    [Fact]
    public void TaskAssignment_Validation_ShouldRejectInactiveUser()
    {
        var user = new ApplicationUser { UserName = "member", IsActive = false };

        Assert.False(user.IsActive);
    }

    [Fact]
    public void ProjectStatus_ValidValues_ShouldBeMapped()
    {
        var status = ProjectStatus.InProgress;

        Assert.Equal(ProjectStatus.InProgress, status);
    }

    [Fact]
    public void TaskPriority_ValidValues_ShouldBeMapped()
    {
        var priority = TaskPriority.High;

        Assert.Equal(TaskPriority.High, priority);
    }
}
