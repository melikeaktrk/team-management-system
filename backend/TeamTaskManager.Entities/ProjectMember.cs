using TeamTaskManager.Core;

namespace TeamTaskManager.Entities;

public class ProjectMember : BaseEntity
{
    public Guid ProjectId { get; set; }
    public Project Project { get; set; } = default!;

    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = default!;

    public MemberRole MemberRole { get; set; } = MemberRole.TeamMember;
    public bool IsActive { get; set; } = true;
    public DateTime JoinedDate { get; set; } = DateTime.UtcNow;

    [Obsolete("Use MemberRole instead.")]
    public string Role
    {
        get => MemberRole.ToString();
        set => MemberRole = Enum.TryParse<MemberRole>(value, true, out var role) ? role : MemberRole.TeamMember;
    }

    [Obsolete("Use JoinedDate instead.")]
    public DateTime JoinedAt
    {
        get => JoinedDate;
        set => JoinedDate = value;
    }
}
