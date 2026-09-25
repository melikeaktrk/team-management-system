using System.ComponentModel.DataAnnotations;
using TeamTaskManager.Core;

namespace TeamTaskManager.Entities;

public class TaskAttachment : BaseEntity
{
    public Guid TaskItemId { get; set; }
    public TaskItem TaskItem { get; set; } = default!;

    public string OriginalFileName { get; set; } = string.Empty;
    public string StoredFileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public Guid? UploadedByUserId { get; set; }
    public ApplicationUser? UploadedByUser { get; set; }

    [Obsolete("Use OriginalFileName instead.")]
    public string FileName
    {
        get => OriginalFileName;
        set => OriginalFileName = value;
    }

    [Obsolete("Use FilePath instead.")]
    public string FileUrl
    {
        get => FilePath;
        set => FilePath = value;
    }
}
