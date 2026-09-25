using TeamTaskManager.Entities;
using TeamTaskManager.DataAccess.Repositories;

namespace TeamTaskManager.DataAccess;

public interface IUnitOfWork : IDisposable
{
    IRepository<Project> Projects { get; }
    IRepository<TaskItem> Tasks { get; }
    IRepository<TaskComment> TaskComments { get; }
    IRepository<ProjectMember> ProjectMembers { get; }
    IRepository<Notification> Notifications { get; }
    IRepository<ActivityLog> ActivityLogs { get; }
    IRepository<ApplicationUser> Users { get; }
    Task<int> SaveChangesAsync();
}
