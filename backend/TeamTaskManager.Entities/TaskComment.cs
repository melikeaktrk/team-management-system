using System.ComponentModel.DataAnnotations;
using TeamTaskManager.Core;

namespace TeamTaskManager.Entities;

public class TaskComment : BaseEntity
{
    public Guid TaskItemId { get; set; }
    public TaskItem TaskItem { get; set; } = default!;

    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = default!;

    [Required]
    [StringLength(1500)]
    public string Content { get; set; } = string.Empty;
}
