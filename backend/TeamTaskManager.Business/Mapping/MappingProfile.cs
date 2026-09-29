using AutoMapper;
using TeamTaskManager.DTO.Auth;
using TeamTaskManager.DTO.Notification;
using TeamTaskManager.DTO.Project;
using TeamTaskManager.DTO.Task;
using TeamTaskManager.DTO.User;
using TeamTaskManager.Entities;

namespace TeamTaskManager.Business.Mapping;

// AutoMapper'ın istek DTO'ları, veritabanı entity'leri ve yanıt DTO'ları arasındaki kuralları.
public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // Kayıt isteğindeki temel alanları Identity kullanıcısına taşır.
        CreateMap<RegisterRequest, ApplicationUser>()
            .ForMember(
                dest => dest.UserName,
                opt => opt.MapFrom(src => src.UserName))
            .ForMember(
                dest => dest.Email,
                opt => opt.MapFrom(src => src.Email));

        CreateMap<UserCreateRequest, ApplicationUser>();
        CreateMap<UserUpdateRequest, ApplicationUser>();

        // Proje oluşturma/güncelleme alan adlarını entity alanlarına eşler.
        CreateMap<ProjectCreateRequest, Project>()
            .ForMember(
                dest => dest.Status,
                opt => opt.MapFrom(_ => "Planning"))
            .ForMember(
                dest => dest.TargetEndDate,
                opt => opt.MapFrom(src => src.DueDate));

        // PROJECT UPDATE
        CreateMap<ProjectUpdateRequest, Project>()
            .ForMember(
                dest => dest.TargetEndDate,
                opt => opt.MapFrom(src => src.DueDate));

        // Entity durumunu ve hedef bitiş tarihini API yanıt modeline eşler.
        CreateMap<Project, ProjectResponse>()
            .ForMember(
                dest => dest.DueDate,
                opt => opt.MapFrom(src => src.TargetEndDate));

        // Üyelik isteğinde rol yoksa varsayılan üye rolünü kullanır.
        CreateMap<ProjectMemberRequest, ProjectMember>()
            .ForMember(
                dest => dest.Role,
                opt => opt.MapFrom(src => src.Role ?? "Member"));

        CreateMap<ProjectMember, ProjectMemberResponse>();

        // Görevlerde varsayılan başlangıç durumu ve kısmi güncelleme eşlemesi.
        CreateMap<TaskItemCreateRequest, TaskItem>()
            .ForMember(
                dest => dest.Status,
                opt => opt.MapFrom(_ => "New"));

        CreateMap<TaskItemUpdateRequest, TaskItem>()
            .ForMember(dest => dest.Title, opt => opt.Condition((src, dest, value) => value is not null))
            .ForMember(dest => dest.Description, opt => opt.Condition((src, dest, value) => value is not null))
            .ForMember(dest => dest.Status, opt => opt.Condition(src => !string.IsNullOrWhiteSpace(src.Status)))
            .ForMember(dest => dest.Priority, opt => opt.Condition(src => !string.IsNullOrWhiteSpace(src.Priority)))
            .ForMember(dest => dest.DueDate, opt => opt.Condition((src, dest, value) => value is not null))
            .ForMember(dest => dest.AssignedToUserId, opt => opt.Condition((src, dest, value) => value is not null));

        CreateMap<TaskItem, TaskItemResponse>();

        // Yorum oluşturma ve yanıt dönüşüm kuralları.
        CreateMap<TaskCommentCreateRequest, TaskComment>();
        CreateMap<TaskComment, TaskCommentResponse>();

        // Bildirim entity'sini dışarıya gönderilecek yanıta dönüştürür.
        CreateMap<Notification, NotificationResponse>();
    }
}
