using System.ComponentModel.DataAnnotations;
using TeamTaskManager.Core;

namespace TeamTaskManager.Entities;

public class Project : BaseEntity
{
    [Required]
    [StringLength(150)]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public ProjectStatus ProjectStatus { get; set; } = ProjectStatus.Planning;
    public DateTime? StartDate { get; set; }
    public DateTime? TargetEndDate { get; set; }
    public Guid? ManagerUserId { get; set; }
    public ApplicationUser? ManagerUser { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public bool IsArchived { get; set; }

    public ICollection<ProjectMember> Members { get; set; } = new List<ProjectMember>();
    public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();

    [Obsolete("Use ProjectStatus instead.")]
    public string Status
    {
        get => ProjectStatus.ToString();
        set => ProjectStatus = Enum.TryParse<ProjectStatus>(value, true, out var status) ? status : ProjectStatus.Planning;
    }
}
