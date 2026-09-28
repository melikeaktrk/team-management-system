using AutoMapper;
using TeamTaskManager.DataAccess;
using TeamTaskManager.DTO.Project;
using TeamTaskManager.DTO.User;
using TeamTaskManager.Entities;

namespace TeamTaskManager.Business.Services;

public interface IProjectService
{
    Task<IEnumerable<ProjectResponse>> GetAllAsync(
        Guid userId,
        bool isAdmin,
        bool isProjectManager);

    Task<ProjectResponse?> GetByIdAsync(Guid id);

    Task<ProjectResponse> CreateAsync(
        ProjectCreateRequest request,
        Guid createdByUserId);

    Task<ProjectResponse?> UpdateAsync(
        Guid id,
        ProjectUpdateRequest request);

    Task<bool> DeleteAsync(Guid id);

    Task<bool> IsManagerAsync(
        Guid projectId,
        Guid userId);

    Task<IEnumerable<UserListResponse>> GetAvailableUsersAsync(
        Guid projectId);

    Task<IEnumerable<ProjectMemberResponse>> GetMembersAsync(
        Guid projectId);

    Task<ProjectMemberResponse> AddMemberAsync(
        Guid projectId,
        ProjectMemberRequest request);
}

public class ProjectService : IProjectService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public ProjectService(
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<IEnumerable<ProjectResponse>> GetAllAsync(
        Guid userId,
        bool isAdmin,
        bool isProjectManager)
    {
        var projects =
            await _unitOfWork.Projects.GetAllAsync();

        // Admin bütün projeleri görebilir.
        if (isAdmin)
        {
            return _mapper.Map<IEnumerable<ProjectResponse>>(
                projects);
        }

        // ProjectManager sadece yönettiği projeleri görebilir.
        if (isProjectManager)
        {
            var managedProjects =
                projects
                    .Where(project =>
                        project.ManagerUserId == userId)
                    .ToList();

            return _mapper.Map<IEnumerable<ProjectResponse>>(
                managedProjects);
        }

        // TeamMember sadece üyesi olduğu projeleri görebilir.
        var allMembers =
            await _unitOfWork.ProjectMembers.GetAllAsync();

        var memberProjectIds =
            allMembers
                .Where(member =>
                    member.UserId == userId)
                .Select(member =>
                    member.ProjectId)
                .ToHashSet();

        var memberProjects =
            projects
                .Where(project =>
                    memberProjectIds.Contains(project.Id))
                .ToList();

        return _mapper.Map<IEnumerable<ProjectResponse>>(
            memberProjects);
    }

    public async Task<ProjectResponse?> GetByIdAsync(Guid id)
    {
        var project =
            await _unitOfWork.Projects.GetByIdAsync(id);

        return project is null
            ? null
            : _mapper.Map<ProjectResponse>(project);
    }

    public async Task<ProjectResponse> CreateAsync(
        ProjectCreateRequest request,
        Guid createdByUserId)
    {
        var project =
            _mapper.Map<Project>(request);

        // Projeyi oluşturan kullanıcı
        project.CreatedByUserId =
            createdByUserId;

        // Proje yöneticisi
        project.ManagerUserId =
            createdByUserId;

        await _unitOfWork.Projects.AddAsync(project);

        await _unitOfWork.SaveChangesAsync();

        return _mapper.Map<ProjectResponse>(
            project);
    }

    public async Task<ProjectResponse?> UpdateAsync(
        Guid id,
        ProjectUpdateRequest request)
    {
        var project =
            await _unitOfWork.Projects.GetByIdAsync(id);

        if (project is null)
            return null;

        _mapper.Map(request, project);

        project.UpdatedAt =
            DateTime.UtcNow;

        _unitOfWork.Projects.Update(project);

        await _unitOfWork.SaveChangesAsync();

        return _mapper.Map<ProjectResponse>(
            project);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var project =
            await _unitOfWork.Projects.GetByIdAsync(id);

        if (project is null)
            return false;

        _unitOfWork.Projects.Delete(project);

        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    public async Task<bool> IsManagerAsync(
        Guid projectId,
        Guid userId)
    {
        var project =
            await _unitOfWork.Projects.GetByIdAsync(
                projectId);

        if (project is null)
            return false;

        return project.ManagerUserId == userId;
    }

    public async Task<IEnumerable<UserListResponse>>
        GetAvailableUsersAsync(Guid projectId)
    {
        var project =
            await _unitOfWork.Projects.GetByIdAsync(
                projectId);

        if (project is null)
            throw new InvalidOperationException(
                "Proje bulunamadı.");

        var allUsers =
            await _unitOfWork.Users.GetAllAsync();

        var allMembers =
            await _unitOfWork.ProjectMembers.GetAllAsync();

        var memberUserIds =
            allMembers
                .Where(member =>
                    member.ProjectId == projectId)
                .Select(member =>
                    member.UserId)
                .ToHashSet();

        var availableUsers =
            allUsers
                .Where(user =>
                    user.IsActive &&
                    !memberUserIds.Contains(user.Id))
                .OrderBy(user =>
                    user.UserName)
                .Select(user => new UserListResponse
                {
                    Id = user.Id,
                    UserName =
                        user.UserName ??
                        string.Empty,
                    Email =
                        user.Email ??
                        string.Empty,
                    FirstName =
                        user.FirstName,
                    LastName =
                        user.LastName,
                    IsActive =
                        user.IsActive,
                    CreatedAt =
                        user.CreatedAt
                })
                .ToList();

        return availableUsers;
    }

    public async Task<IEnumerable<ProjectMemberResponse>>
        GetMembersAsync(Guid projectId)
    {
        var project =
            await _unitOfWork.Projects.GetByIdAsync(
                projectId);

        if (project is null)
            throw new InvalidOperationException(
                "Proje bulunamadı.");

        var allMembers =
            await _unitOfWork.ProjectMembers.GetAllAsync();

        var projectMembers =
            allMembers
                .Where(member =>
                    member.ProjectId == projectId)
                .ToList();
        var users = (await _unitOfWork.Users.GetAllAsync()).ToDictionary(user => user.Id);
        var responses = _mapper.Map<List<ProjectMemberResponse>>(projectMembers);
        foreach (var response in responses)
        {
            if (!users.TryGetValue(response.UserId, out var user)) continue;
            response.UserName = user.UserName;
            response.Email = user.Email;
            response.FirstName = user.FirstName;
            response.LastName = user.LastName;
            response.IsActive = projectMembers.First(member => member.Id == response.Id).IsActive && user.IsActive;
        }
        return responses;
    }

    public async Task<ProjectMemberResponse>
        AddMemberAsync(
            Guid projectId,
            ProjectMemberRequest request)
    {
        var project =
            await _unitOfWork.Projects.GetByIdAsync(
                projectId);

        if (project is null)
            throw new InvalidOperationException(
                "Proje bulunamadı.");

        var user =
            await _unitOfWork.Users.GetByIdAsync(
                request.UserId);

        if (user is null)
            throw new InvalidOperationException(
                "Kullanıcı bulunamadı.");

        if (!user.IsActive)
            throw new InvalidOperationException(
                "Pasif kullanıcı projeye eklenemez.");

        var allMembers =
            await _unitOfWork.ProjectMembers.GetAllAsync();

        var alreadyMember =
            allMembers.Any(member =>
                member.ProjectId == projectId &&
                member.UserId == request.UserId);

        if (alreadyMember)
            throw new InvalidOperationException(
                "Kullanıcı zaten bu projeye eklenmiş.");

        var member =
            _mapper.Map<ProjectMember>(request);

        member.ProjectId =
            projectId;

        member.JoinedAt =
            DateTime.UtcNow;

        await _unitOfWork.ProjectMembers.AddAsync(
            member);

        await _unitOfWork.SaveChangesAsync();

        return _mapper.Map<ProjectMemberResponse>(
            member);
    }
}
