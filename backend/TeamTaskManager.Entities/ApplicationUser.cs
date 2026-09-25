using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace TeamTaskManager.Entities;

public class ApplicationUser : IdentityUser<Guid>
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? ProfileImageUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedDate { get; set; }
    public DateTime? LastLoginAt { get; set; }

    [NotMapped]
    public DateTime CreatedAt
    {
        get => CreatedDate;
        set => CreatedDate = value;
    }

    [NotMapped]
    public DateTime? UpdatedAt
    {
        get => UpdatedDate;
        set => UpdatedDate = value;
    }

    public ICollection<Project> ManagedProjects { get; set; } = new List<Project>();
    public ICollection<ProjectMember> ProjectMemberships { get; set; } = new List<ProjectMember>();
    public ICollection<TaskItem> AssignedTasks { get; set; } = new List<TaskItem>();
    public ICollection<TaskItem> CreatedTasks { get; set; } = new List<TaskItem>();
    public ICollection<TaskComment> TaskComments { get; set; } = new List<TaskComment>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    public ICollection<ActivityLog> ActivityLogs { get; set; } = new List<ActivityLog>();
    public ICollection<TaskAttachment> UploadedAttachments { get; set; } = new List<TaskAttachment>();
}
