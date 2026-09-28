using AutoMapper;
using TeamTaskManager.DataAccess;
using TeamTaskManager.DTO.Notification;

namespace TeamTaskManager.Business.Services;

public interface INotificationService
{
    Task<IEnumerable<NotificationResponse>> GetForUserAsync(Guid userId);
}

public class NotificationService : INotificationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public NotificationService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<IEnumerable<NotificationResponse>> GetForUserAsync(Guid userId)
    {
        var notifications = (await _unitOfWork.Notifications.GetAllAsync())
            .Where(notification => notification.UserId == userId)
            .OrderByDescending(notification => notification.CreatedAt)
            .ToList();

        return _mapper.Map<IEnumerable<NotificationResponse>>(notifications);
    }
}
