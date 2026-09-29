using AutoMapper;
using TeamTaskManager.Business.Mapping;
using TeamTaskManager.Business.Services;
using TeamTaskManager.DataAccess;
using TeamTaskManager.DataAccess.Repositories;
using TeamTaskManager.DTO.Task;
using TeamTaskManager.Entities;

namespace TeamTaskManager.Tests;

// Gerçek SQL yerine bellek içi sahte UnitOfWork kullanarak servis akışlarını sınar.
public class TaskServiceFlowTests
{
    // Gerçek AutoMapper profiliyle test edilen TaskService örneği üretir.
    private static TaskService CreateService(InMemoryUnitOfWork unitOfWork)
    {
        var mapper = new MapperConfiguration(configuration =>
            configuration.AddProfile<MappingProfile>()).CreateMapper();
        return new TaskService(unitOfWork, mapper);
    }

    // Yeni görevin New durumuyla açıldığını ve istenen önceliğin korunduğunu kontrol eder.
    [Fact]
    public async Task CreateAsync_UsesNewStatusAndRequestedPriority()
    {
        var unitOfWork = new InMemoryUnitOfWork();
        var service = CreateService(unitOfWork);

        var created = await service.CreateAsync(new TaskItemCreateRequest
        {
            ProjectId = Guid.NewGuid(),
            Title = "First task",
            Priority = "High"
        });

        Assert.Equal("New", created.Status);
        Assert.Equal("High", created.Priority);
        Assert.Single(unitOfWork.TaskRepository.Items);
    }

    // Güncellemede izin verilen durum değişikliğinin tarih alanına etkisini kontrol eder.
    [Fact]
    public async Task UpdateAsync_AppliesAllowedStatusAndCompletedDate()
    {
        var task = new TaskItem { Title = "Before", Status = TeamTaskManager.Entities.TaskStatus.New };
        var unitOfWork = new InMemoryUnitOfWork(task);
        var service = CreateService(unitOfWork);

        var updated = await service.UpdateAsync(task.Id, new TaskItemUpdateRequest
        {
            Title = "After",
            Status = "InProgress"
        });

        Assert.NotNull(updated);
        Assert.Equal("After", updated.Title);
        Assert.Equal("InProgress", updated.Status);
        Assert.Null(task.CompletedDate);
    }

    // Durum aynı kaldığında diğer görev alanlarının güncellenebildiğini doğrular.
    [Fact]
    public async Task UpdateAsync_AllowsOtherFieldChangesWhenStatusIsUnchanged()
    {
        var task = new TaskItem { Title = "Before", Status = TeamTaskManager.Entities.TaskStatus.New };
        var service = CreateService(new InMemoryUnitOfWork(task));

        var updated = await service.UpdateAsync(task.Id, new TaskItemUpdateRequest
        {
            Title = "After",
            Status = "New"
        });

        Assert.NotNull(updated);
        Assert.Equal("After", updated.Title);
        Assert.Equal("New", updated.Status);
    }

    // Tamamlanma tarihinin atanmasını ve izin verilmeyen geçişin hata vermesini sınar.
    [Fact]
    public async Task UpdateStatusAsync_SetsCompletedDate_AndRejectsDisallowedTransition()
    {
        var task = new TaskItem { Title = "Done", Status = TeamTaskManager.Entities.TaskStatus.InProgress };
        var service = CreateService(new InMemoryUnitOfWork(task));

        var completed = await service.UpdateStatusAsync(task.Id, TeamTaskManager.Entities.TaskStatus.Completed);
        Assert.NotNull(completed);
        Assert.Equal("Completed", completed.Status);
        Assert.NotNull(task.CompletedDate);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.UpdateStatusAsync(task.Id, TeamTaskManager.Entities.TaskStatus.Waiting));
    }

    // Bildirim servisinin başka kullanıcının bildirimini sonuçlara katmadığını sınar.
    [Fact]
    public async Task NotificationService_ReturnsOnlyCurrentUsersNotifications()
    {
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var unitOfWork = new InMemoryUnitOfWork();
        unitOfWork.NotificationRepository.Items.Add(new Notification
        {
            UserId = userId,
            Title = "Mine",
            Message = "For current user"
        });
        unitOfWork.NotificationRepository.Items.Add(new Notification
        {
            UserId = otherUserId,
            Title = "Other",
            Message = "For someone else"
        });
        var mapper = new MapperConfiguration(configuration =>
            configuration.AddProfile<MappingProfile>()).CreateMapper();
        var service = new NotificationService(unitOfWork, mapper);

        var result = await service.GetForUserAsync(userId);

        Assert.Single(result);
        Assert.Equal("Mine", result.Single().Title);
    }

    // Her entity türü için bellek içi repository sağlayan test doubles sınıfı.
    private sealed class InMemoryUnitOfWork : IUnitOfWork
    {
        public InMemoryRepository<Project> ProjectRepository { get; } = new(x => x.Id);
        public InMemoryRepository<TaskItem> TaskRepository { get; } = new(x => x.Id);
        public InMemoryRepository<TaskComment> CommentRepository { get; } = new(x => x.Id);
        public InMemoryRepository<TaskAttachment> AttachmentRepository { get; } = new(x => x.Id);
        public InMemoryRepository<ProjectMember> MemberRepository { get; } = new(x => x.Id);
        public InMemoryRepository<Notification> NotificationRepository { get; } = new(x => x.Id);
        public InMemoryRepository<ActivityLog> ActivityRepository { get; } = new(x => x.Id);
        public InMemoryRepository<ApplicationUser> UserRepository { get; } = new(x => x.Id);

        public InMemoryUnitOfWork(TaskItem? task = null)
        {
            if (task is not null) TaskRepository.Items.Add(task);
        }

        public IRepository<Project> Projects => ProjectRepository;
        public IRepository<TaskItem> Tasks => TaskRepository;
        public IRepository<TaskComment> TaskComments => CommentRepository;
        public IRepository<TaskAttachment> TaskAttachments => AttachmentRepository;
        public IRepository<ProjectMember> ProjectMembers => MemberRepository;
        public IRepository<Notification> Notifications => NotificationRepository;
        public IRepository<ActivityLog> ActivityLogs => ActivityRepository;
        public IRepository<ApplicationUser> Users => UserRepository;
        public Task<int> SaveChangesAsync() => Task.FromResult(1);
        public void Dispose() { }
    }

    // Liste üzerinde temel repository davranışlarını taklit eder; gerçek EF çalıştırmaz.
    private sealed class InMemoryRepository<T>(Func<T, Guid> getId) : IRepository<T> where T : class
    {
        public List<T> Items { get; } = [];
        public Task<T?> GetByIdAsync(Guid id) => Task.FromResult(Items.FirstOrDefault(item => getId(item) == id));
        public Task<IEnumerable<T>> GetAllAsync() => Task.FromResult<IEnumerable<T>>(Items.ToList());
        public Task AddAsync(T entity) { Items.Add(entity); return Task.CompletedTask; }
        public void Update(T entity) { }
        public void Delete(T entity) => Items.Remove(entity);
    }
}
