using System.ComponentModel.DataAnnotations;

namespace TeamTaskManager.DTO.Project;

public class ProjectMemberRequest : IValidatableObject
{
    public Guid UserId { get; set; }
    [RegularExpression("^(ProjectManager|TeamMember)$", ErrorMessage = "Geçersiz proje üye rolü.")]
    public string? Role { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (UserId == Guid.Empty)
            yield return new ValidationResult("Geçerli bir kullanıcı seçilmelidir.", [nameof(UserId)]);
    }
}

public class ProjectMemberResponse
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public Guid UserId { get; set; }
    public string Role { get; set; } = string.Empty;
    public string? UserName { get; set; }
    public string? Email { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public bool IsActive { get; set; }
    public DateTime JoinedAt { get; set; }
}

public class ProjectUpdateRequest
{
    [StringLength(150, MinimumLength = 1)]
    [RegularExpression(@".*\S.*", ErrorMessage = "Proje adı boş olamaz.")]
    public string? Name { get; set; }
    [StringLength(2000)]
    public string? Description { get; set; }
    [RegularExpression("^(Planning|InProgress|Completed|Cancelled)$", ErrorMessage = "Geçersiz proje durumu.")]
    public string? Status { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? DueDate { get; set; }
}
