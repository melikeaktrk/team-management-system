using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TeamTaskManager.API.Controllers;
using TeamTaskManager.Business.Services;
using TeamTaskManager.DTO.Project;
using TeamTaskManager.DTO.Task;
using TeamTaskManager.DTO.User;

namespace TeamTaskManager.Tests;

public class AuthorizationControllerTests
{
    private static readonly Guid OwnProjectId = Guid.NewGuid();
    private static readonly Guid OtherProjectId = Guid.NewGuid();
    private static readonly Guid MemberId = Guid.NewGuid();
    private static readonly Guid OtherMemberId = Guid.NewGuid();
    private static readonly Guid OwnTaskId = Guid.NewGuid();
    private static readonly Guid OtherTaskId = Guid.NewGuid();

    [Fact]
    public async Task TeamMember_CanListOnlyAssignedTasks()
    {
        var controller = CreateTaskController(MemberId, "TeamMember");

        var result = await controller.GetByProject(OwnProjectId);

        var tasks = Assert.IsType<OkObjectResult>(result).Value as IEnumerable<TaskItemResponse>;
        Assert.NotNull(tasks);
        Assert.Single(tasks);
        Assert.Equal(MemberId, tasks.Single().AssignedToUserId);
    }

    [Fact]
    public async Task ProjectManager_CannotListAnotherManagersTasks()
    {
        var controller = CreateTaskController(MemberId, "ProjectManager");

        var result = await controller.GetByProject(OtherProjectId);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task Admin_CanReadTaskOutsideOwnProjects()
    {
        var controller = CreateTaskController(MemberId, "Admin");

        var result = await controller.GetById(OtherTaskId);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task TeamMember_CannotReadOrDeleteAnotherUsersTask()
    {
        var controller = CreateTaskController(MemberId, "TeamMember");

        Assert.IsType<ForbidResult>(await controller.GetById(OtherTaskId));
        Assert.IsType<ForbidResult>(await controller.Delete(OtherTaskId));
    }

    [Fact]
    public async Task TeamMember_CanUpdateOnlyStatusOfAssignedTask()
    {
        var controller = CreateTaskController(MemberId, "TeamMember");

        var result = await controller.Update(OwnTaskId, new TaskItemUpdateRequest { Status = "InProgress" });

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task TeamMember_CannotChangeOtherTaskFields()
    {
        var controller = CreateTaskController(MemberId, "TeamMember");

        var result = await controller.Update(OwnTaskId, new TaskItemUpdateRequest
        {
            Status = "InProgress",
            Title = "Unauthorized change"
        });

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task TeamMember_CannotCreateTasksOrAddCommentsToUnassignedTasks()
    {
        var controller = CreateTaskController(MemberId, "TeamMember");

        Assert.IsType<ForbidResult>(await controller.Create(new TaskItemCreateRequest { ProjectId = OwnProjectId, Title = "Task" }));
        Assert.IsType<ForbidResult>(await controller.AddComment(OtherTaskId, new TaskCommentCreateRequest { Content = "No access" }));
    }

    [Fact]
    public async Task ProjectMembers_AreLimitedToAccessibleProjects()
    {
        var controller = new ProjectController(new FakeProjectService());
        SetUser(controller, MemberId, "TeamMember");

        Assert.IsType<OkObjectResult>(await controller.GetMembers(OwnProjectId));
        Assert.IsType<ForbidResult>(await controller.GetMembers(OtherProjectId));
    }

    [Fact]
    public async Task ProjectManager_CannotReadAnotherProjectsMembers()
    {
        var controller = new ProjectController(new FakeProjectService());
        SetUser(controller, MemberId, "ProjectManager");

        Assert.IsType<ForbidResult>(await controller.GetMembers(OtherProjectId));
    }

    [Fact]
    public async Task Admin_CanReadAnyProjectsMembers_AndManagerCanReadOwn()
    {
        var adminController = new ProjectController(new FakeProjectService());
        SetUser(adminController, MemberId, "Admin");
        Assert.IsType<OkObjectResult>(await adminController.GetMembers(OtherProjectId));

        var managerController = new ProjectController(new FakeProjectService());
        SetUser(managerController, MemberId, "ProjectManager");
        Assert.IsType<OkObjectResult>(await managerController.GetMembers(OwnProjectId));
    }

    [Fact]
    public async Task TeamMember_CannotCreateProjects()
    {
        var controller = new ProjectController(new FakeProjectService());
        SetUser(controller, MemberId, "TeamMember");

        Assert.IsType<ForbidResult>(await controller.Create(new ProjectCreateRequest { Name = "No" }));
    }

    private static TaskController CreateTaskController(Guid userId, string role)
    {
        var controller = new TaskController(new FakeTaskService(), new FakeProjectService(), new FakeAttachmentService());
        SetUser(controller, userId, role);
        return controller;
    }

    private static void SetUser(ControllerBase controller, Guid userId, string role)
    {
        var identity = new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, role)
        ], "UnitTest");
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };
    }

    private sealed class FakeProjectService : IProjectService
    {
        public Task<IEnumerable<ProjectResponse>> GetAllAsync(Guid userId, bool isAdmin, bool isProjectManager) => Task.FromResult<IEnumerable<ProjectResponse>>([]);
        public Task<ProjectResponse?> GetByIdAsync(Guid id) => Task.FromResult<ProjectResponse?>(new ProjectResponse { Id = id });
        public Task<ProjectResponse> CreateAsync(ProjectCreateRequest request, Guid createdByUserId) => Task.FromResult(new ProjectResponse());
        public Task<ProjectResponse?> UpdateAsync(Guid id, ProjectUpdateRequest request) => Task.FromResult<ProjectResponse?>(new ProjectResponse { Id = id });
        public Task<bool> DeleteAsync(Guid id) => Task.FromResult(true);
        public Task<bool> IsManagerAsync(Guid projectId, Guid userId) => Task.FromResult(projectId == OwnProjectId);
        public Task<IEnumerable<UserListResponse>> GetAvailableUsersAsync(Guid projectId) => Task.FromResult<IEnumerable<UserListResponse>>([]);
        public Task<IEnumerable<ProjectMemberResponse>> GetMembersAsync(Guid projectId) => Task.FromResult<IEnumerable<ProjectMemberResponse>>(
            projectId == OwnProjectId ? [new ProjectMemberResponse { UserId = MemberId }] : [new ProjectMemberResponse { UserId = OtherMemberId }]);
        public Task<ProjectMemberResponse> AddMemberAsync(Guid projectId, ProjectMemberRequest request) => Task.FromResult(new ProjectMemberResponse());
    }

    private sealed class FakeTaskService : ITaskService
    {
        private static TaskItemResponse Item(Guid id) => new()
        {
            Id = id,
            ProjectId = id == OwnTaskId ? OwnProjectId : OtherProjectId,
            AssignedToUserId = id == OwnTaskId ? MemberId : OtherMemberId,
            Title = "Task",
            Status = "New",
            Priority = "Medium"
        };

        public Task<IEnumerable<TaskItemResponse>> GetByProjectAsync(Guid projectId) => Task.FromResult<IEnumerable<TaskItemResponse>>(
            [Item(OwnTaskId), Item(OtherTaskId)]);
        public Task<TaskItemResponse?> GetByIdAsync(Guid id) => Task.FromResult<TaskItemResponse?>(id == OwnTaskId || id == OtherTaskId ? Item(id) : null);
        public Task<TaskItemResponse> CreateAsync(TaskItemCreateRequest request) => Task.FromResult(Item(Guid.NewGuid()));
        public Task<TaskItemResponse?> UpdateAsync(Guid id, TaskItemUpdateRequest request) => Task.FromResult<TaskItemResponse?>(Item(id));
        public Task<TaskItemResponse?> UpdateStatusAsync(Guid id, TeamTaskManager.Entities.TaskStatus status) => Task.FromResult<TaskItemResponse?>(Item(id));
        public Task<bool> DeleteAsync(Guid id) => Task.FromResult(true);
        public Task<IEnumerable<TaskCommentResponse>> GetCommentsAsync(Guid taskId) => Task.FromResult<IEnumerable<TaskCommentResponse>>([]);
        public Task<TaskCommentResponse> AddCommentAsync(Guid taskId, Guid userId, TaskCommentCreateRequest request) => Task.FromResult(new TaskCommentResponse { TaskItemId = taskId });
    }

    private sealed class FakeAttachmentService : ITaskAttachmentService
    {
        public Task<IEnumerable<TaskAttachmentResponse>> GetByTaskAsync(Guid taskId) => Task.FromResult<IEnumerable<TaskAttachmentResponse>>([]);
        public Task<TaskAttachmentResponse?> GetByIdAsync(Guid attachmentId) => Task.FromResult<TaskAttachmentResponse?>(null);
        public Task<TaskAttachmentResponse> UploadAsync(Guid taskId, Guid userId, Microsoft.AspNetCore.Http.IFormFile file) => Task.FromResult(new TaskAttachmentResponse());
        public Task<(byte[] Content, string ContentType, string FileName)?> DownloadAsync(Guid attachmentId) => Task.FromResult<(byte[] Content, string ContentType, string FileName)?>(null);
    }
}
