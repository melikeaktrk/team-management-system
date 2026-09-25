namespace TeamTaskManager.DTO.Project;

/// <summary>
/// Yeni proje oluşturma isteği modeli.
/// API katmanı bu DTO'yu alır ve Business katmanına iletir.
/// </summary>
public class ProjectCreateRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? DueDate { get; set; }
}
