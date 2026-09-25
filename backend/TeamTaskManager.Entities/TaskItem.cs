using System.ComponentModel.DataAnnotations;
using TeamTaskManager.Core;

namespace TeamTaskManager.Entities;

public class TaskItem : BaseEntity
{
    public Guid ProjectId { get; set; }
    public Project Project { get; set; } = default!;

    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public TaskStatus Status { get; set; } = TaskStatus.New;
    public TaskPriority Priority { get; set; } = TaskPriority.Medium;
    public DateTime? StartDate { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime? CompletedDate { get; set; }

    public Guid? AssignedToUserId { get; set; }
    public ApplicationUser? AssignedToUser { get; set; }

    public Guid? CreatedByUserId { get; set; }
    public ApplicationUser? CreatedByUser { get; set; }

    public ICollection<TaskComment> Comments { get; set; } = new List<TaskComment>();
    public ICollection<TaskAttachment> Attachments { get; set; } = new List<TaskAttachment>();

    [Obsolete("Use Status instead.")]
    public string LegacyStatus
    {
        get => Status.ToString();
        set => Status = Enum.TryParse<TaskStatus>(value, true, out var status) ? status : TaskStatus.New;
    }

    [Obsolete("Use Priority instead.")]
    public string LegacyPriority
    {
        get => Priority.ToString();
        set => Priority = Enum.TryParse<TaskPriority>(value, true, out var priority) ? priority : TaskPriority.Medium;
    }
}
