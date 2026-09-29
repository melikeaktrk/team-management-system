using TeamTaskManager.Core;

namespace TeamTaskManager.Entities;

// Kullanıcıya gösterilecek bildirimi ve okundu/ilişkili kayıt bilgisini tutar.
public class Notification : BaseEntity
{
    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = default!;

    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public NotificationType Type { get; set; } = NotificationType.Info;
    public Guid? RelatedEntityId { get; set; }
    public bool IsRead { get; set; }

    // Eski string bildirim türünü mevcut enum Type alanına dönüştürür.
    [Obsolete("Use Type instead.")]
    public string LegacyType
    {
        get => Type.ToString();
        set => Type = Enum.TryParse<NotificationType>(value, true, out var type) ? type : NotificationType.Info;
    }
}
