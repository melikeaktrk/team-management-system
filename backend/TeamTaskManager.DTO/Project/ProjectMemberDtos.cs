namespace TeamTaskManager.DTO.Project;

public class ProjectMemberRequest
{
    public Guid UserId { get; set; }
    public string? Role { get; set; }
}

public class ProjectMemberResponse
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public Guid UserId { get; set; }
    public string Role { get; set; } = string.Empty;
    public DateTime JoinedAt { get; set; }
}

public class ProjectUpdateRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Status { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? DueDate { get; set; }
}
