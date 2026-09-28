using AutoMapper;
using TeamTaskManager.DTO.Auth;
using TeamTaskManager.DTO.Notification;
using TeamTaskManager.DTO.Project;
using TeamTaskManager.DTO.Task;
using TeamTaskManager.DTO.User;
using TeamTaskManager.Entities;

namespace TeamTaskManager.Business.Mapping;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<RegisterRequest, ApplicationUser>()
            .ForMember(
                dest => dest.UserName,
                opt => opt.MapFrom(src => src.UserName))
            .ForMember(
                dest => dest.Email,
                opt => opt.MapFrom(src => src.Email));

        CreateMap<UserCreateRequest, ApplicationUser>();
        CreateMap<UserUpdateRequest, ApplicationUser>();

        // PROJECT CREATE
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

        // PROJECT RESPONSE
        CreateMap<Project, ProjectResponse>()
            .ForMember(
                dest => dest.DueDate,
                opt => opt.MapFrom(src => src.TargetEndDate));

        // PROJECT MEMBER
        CreateMap<ProjectMemberRequest, ProjectMember>()
            .ForMember(
                dest => dest.Role,
                opt => opt.MapFrom(src => src.Role ?? "Member"));

        CreateMap<ProjectMember, ProjectMemberResponse>();

        // TASK
        CreateMap<TaskItemCreateRequest, TaskItem>()
            .ForMember(
                dest => dest.Status,
                opt => opt.MapFrom(_ => "New"));

        CreateMap<TaskItemUpdateRequest, TaskItem>()
            .ForMember(dest => dest.Status, opt => opt.Condition(src => !string.IsNullOrWhiteSpace(src.Status)))
            .ForMember(dest => dest.Priority, opt => opt.Condition(src => !string.IsNullOrWhiteSpace(src.Priority)));

        CreateMap<TaskItem, TaskItemResponse>();

        // COMMENTS
        CreateMap<TaskCommentCreateRequest, TaskComment>();
        CreateMap<TaskComment, TaskCommentResponse>();

        // NOTIFICATIONS
        CreateMap<Notification, NotificationResponse>();
    }
}
