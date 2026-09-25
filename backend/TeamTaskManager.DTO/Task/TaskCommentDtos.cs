namespace TeamTaskManager.DTO.Task;

public class TaskCommentCreateRequest
{
    public Guid TaskItemId { get; set; }
    public string Content { get; set; } = string.Empty;
}

public class TaskCommentResponse
{
    public Guid Id { get; set; }
    public Guid TaskItemId { get; set; }
    public Guid UserId { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class TaskItemUpdateRequest
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Status { get; set; }
    public string? Priority { get; set; }
    public DateTime? DueDate { get; set; }
    public Guid? AssignedToUserId { get; set; }
}
