using System.ComponentModel.DataAnnotations;

namespace TeamTaskManager.DTO.Project;

/// <summary>
/// Yeni proje oluşturma isteği modeli.
/// API katmanı bu DTO'yu alır ve Business katmanına iletir.
/// </summary>
public class ProjectCreateRequest
{
    [Required]
    [StringLength(150, MinimumLength = 1)]
    [RegularExpression(@".*\S.*", ErrorMessage = "Proje adı boş olamaz.")]
    public string Name { get; set; } = string.Empty;
    [StringLength(2000)]
    public string? Description { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? DueDate { get; set; }
}
