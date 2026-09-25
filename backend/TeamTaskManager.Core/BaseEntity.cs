using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TeamTaskManager.Core;

public abstract class BaseEntity
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedDate { get; set; }
    public bool IsDeleted { get; set; }

    [NotMapped]
    public DateTime CreatedAt
    {
        get => CreatedDate;
        set => CreatedDate = value;
    }

    [NotMapped]
    public DateTime? UpdatedAt
    {
        get => UpdatedDate;
        set => UpdatedDate = value;
    }
}
