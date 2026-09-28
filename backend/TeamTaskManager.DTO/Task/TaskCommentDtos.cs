using System.ComponentModel.DataAnnotations;

namespace TeamTaskManager.DTO.Task;

public class TaskCommentCreateRequest
{
    public Guid TaskItemId { get; set; }
    [Required]
    [StringLength(1500, MinimumLength = 1)]
    [RegularExpression(@".*\S.*", ErrorMessage = "Yorum boş olamaz.")]
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

public class TaskAttachmentResponse
{
    public Guid Id { get; set; }
    public Guid TaskItemId { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public Guid? UploadedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class TaskItemUpdateRequest : IValidatableObject
{
    [StringLength(200, MinimumLength = 1)]
    [RegularExpression(@".*\S.*", ErrorMessage = "Görev başlığı boş olamaz.")]
    public string? Title { get; set; }
    [StringLength(4000)]
    public string? Description { get; set; }
    [RegularExpression("^(New|InProgress|Waiting|Completed|Cancelled)$", ErrorMessage = "Geçersiz görev durumu.")]
    public string? Status { get; set; }
    [RegularExpression("^(Low|Medium|High)$", ErrorMessage = "Geçersiz görev önceliği.")]
    public string? Priority { get; set; }
    public DateTime? DueDate { get; set; }
    public Guid? AssignedToUserId { get; set; }
    public bool ClearAssignment { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (AssignedToUserId == Guid.Empty)
            yield return new ValidationResult("Atanan kullanıcı kimliği geçersiz.", [nameof(AssignedToUserId)]);
    }
}
