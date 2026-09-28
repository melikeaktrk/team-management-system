namespace TeamTaskManager.DTO.Task;

/// <summary>
/// Görev yanıt modeli.
/// API çıktısında kullanılacak görev detaylarını içerir.
/// </summary>
public class TaskItemResponse
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public DateTime? DueDate { get; set; }
    public DateTime? CompletedDate { get; set; }
    public Guid? AssignedToUserId { get; set; }
    public DateTime CreatedAt { get; set; }
}
