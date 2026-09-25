using TeamTaskManager.Core;

namespace TeamTaskManager.Entities;

public class ActivityLog : BaseEntity
{
    public Guid? UserId { get; set; }
    public ApplicationUser? User { get; set; }

    public string EntityType { get; set; } = string.Empty;
    public Guid? EntityId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? Description { get; set; }
}
