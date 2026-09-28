using System.ComponentModel.DataAnnotations;

namespace TeamTaskManager.DTO.Task;

/// <summary>
/// Görev oluşturma isteği modeli.
/// API üzerinden gelen görev verilerini temsil eder.
/// </summary>
public class TaskItemCreateRequest : IValidatableObject
{
    public Guid ProjectId { get; set; }
    [Required]
    [StringLength(200, MinimumLength = 1)]
    [RegularExpression(@".*\S.*", ErrorMessage = "Görev başlığı boş olamaz.")]
    public string Title { get; set; } = string.Empty;
    [StringLength(4000)]
    public string? Description { get; set; }
    [RegularExpression("^(Low|Medium|High)$", ErrorMessage = "Geçersiz görev önceliği.")]
    public string Priority { get; set; } = "Medium";
    public DateTime? DueDate { get; set; }
    public Guid? AssignedToUserId { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (ProjectId == Guid.Empty)
            yield return new ValidationResult("Geçerli bir proje seçilmelidir.", [nameof(ProjectId)]);
        if (AssignedToUserId == Guid.Empty)
            yield return new ValidationResult("Atanan kullanıcı kimliği geçersiz.", [nameof(AssignedToUserId)]);
    }
}
