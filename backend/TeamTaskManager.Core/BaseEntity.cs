using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TeamTaskManager.Core;

// Ortak kimlik, oluşturulma/güncellenme zamanı ve silinme alanlarını entity'lere sağlar.
public abstract class BaseEntity
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedDate { get; set; }
    public bool IsDeleted { get; set; }

    // CreatedAt eski/alternatif adıdır; NotMapped olduğu için ayrı DB sütunu oluşturmaz.
    [NotMapped]
    public DateTime CreatedAt
    {
        get => CreatedDate;
        set => CreatedDate = value;
    }

    // UpdatedAt eski/alternatif adıdır; gerçek değer UpdatedDate alanında saklanır.
    [NotMapped]
    public DateTime? UpdatedAt
    {
        get => UpdatedDate;
        set => UpdatedDate = value;
    }
}
