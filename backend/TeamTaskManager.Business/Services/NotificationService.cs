using AutoMapper;
using TeamTaskManager.DataAccess;
using TeamTaskManager.DTO.Notification;

namespace TeamTaskManager.Business.Services;

// Bildirim okuma ve okundu işaretleme işlemlerinin servis sözleşmesi.
public interface INotificationService
{
    Task<IEnumerable<NotificationResponse>> GetForUserAsync(Guid userId);
    Task<NotificationResponse?> MarkAsReadAsync(Guid notificationId, Guid userId);
    Task<int> MarkAllAsReadAsync(Guid userId);
}

// Bildirimleri yalnızca ilgili kullanıcı kimliğine göre seçip günceller.
public class NotificationService : INotificationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public NotificationService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    // Kullanıcının bildirimlerini en yeniden eskiye sıralayıp DTO'ya dönüştürür.
    public async Task<IEnumerable<NotificationResponse>> GetForUserAsync(Guid userId)
    {
        var notifications = (await _unitOfWork.Notifications.GetAllAsync())
            .Where(notification => notification.UserId == userId)
            .OrderByDescending(notification => notification.CreatedAt)
            .ToList();

        return _mapper.Map<IEnumerable<NotificationResponse>>(notifications);
    }

    // Bildirim başka kullanıcıya aitse değiştirmeden null döndürür.
    public async Task<NotificationResponse?> MarkAsReadAsync(Guid notificationId, Guid userId)
    {
        var notification = await _unitOfWork.Notifications.GetByIdAsync(notificationId);
        if (notification is null || notification.UserId != userId)
            return null;

        notification.IsRead = true;
        _unitOfWork.Notifications.Update(notification);
        await _unitOfWork.SaveChangesAsync();
        return _mapper.Map<NotificationResponse>(notification);
    }

    // Kullanıcının okunmamış kayıtlarını tek seferde işaretler ve adedi döndürür.
    public async Task<int> MarkAllAsReadAsync(Guid userId)
    {
        var notifications = (await _unitOfWork.Notifications.GetAllAsync())
            .Where(notification => notification.UserId == userId && !notification.IsRead)
            .ToList();

        foreach (var notification in notifications)
        {
            notification.IsRead = true;
            _unitOfWork.Notifications.Update(notification);
        }

        if (notifications.Count > 0)
            await _unitOfWork.SaveChangesAsync();

        return notifications.Count;
    }
}
