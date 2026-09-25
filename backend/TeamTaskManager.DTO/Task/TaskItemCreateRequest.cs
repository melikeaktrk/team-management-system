namespace TeamTaskManager.DTO.Task;

/// <summary>
/// Görev oluşturma isteği modeli.
/// API üzerinden gelen görev verilerini temsil eder.
/// </summary>
public class TaskItemCreateRequest
{
    public Guid ProjectId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Priority { get; set; } = "Medium";
    public DateTime? DueDate { get; set; }
    public Guid? AssignedToUserId { get; set; }
}
