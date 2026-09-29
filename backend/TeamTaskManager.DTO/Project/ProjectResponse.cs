namespace TeamTaskManager.DTO.Project;

/// <summary>
/// Proje detay yanıtı modeli.
/// API servislerinden dışarıya döndürülecek veriyi temsil eder.
/// </summary>
public class ProjectResponse
{
    // İstemciye yalnızca proje özetini verir; EF entity/navigation nesnelerini dışarı taşımaz.
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? StartDate { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime CreatedAt { get; set; }
}
