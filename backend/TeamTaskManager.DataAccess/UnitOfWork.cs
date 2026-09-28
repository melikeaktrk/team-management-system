using TeamTaskManager.DataAccess.Repositories;
using TeamTaskManager.Entities;

namespace TeamTaskManager.DataAccess;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;
    public IRepository<Project> Projects { get; }
    public IRepository<TaskItem> Tasks { get; }
    public IRepository<TaskComment> TaskComments { get; }
    public IRepository<TaskAttachment> TaskAttachments { get; }
    public IRepository<ProjectMember> ProjectMembers { get; }
    public IRepository<Notification> Notifications { get; }
    public IRepository<ActivityLog> ActivityLogs { get; }
    public IRepository<ApplicationUser> Users { get; }

    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context;
        Projects = new Repository<Project>(_context);
        Tasks = new Repository<TaskItem>(_context);
        TaskComments = new Repository<TaskComment>(_context);
        TaskAttachments = new Repository<TaskAttachment>(_context);
        ProjectMembers = new Repository<ProjectMember>(_context);
        Notifications = new Repository<Notification>(_context);
        ActivityLogs = new Repository<ActivityLog>(_context);
        Users = new Repository<ApplicationUser>(_context);
    }

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
