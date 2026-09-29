using AutoMapper;
using TeamTaskManager.DataAccess;
using TeamTaskManager.DTO.Project;
using TeamTaskManager.DTO.User;
using TeamTaskManager.Entities;

namespace TeamTaskManager.Business.Services;

// Proje listeleme, CRUD, üyelik ve yönetici sahipliği işlemlerinin sözleşmesi.
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

    Task<bool> RemoveMemberAsync(Guid projectId, Guid userId);
}

// Proje kurallarını uygular; kalıcı değişiklikleri Unit of Work üzerinden kaydeder.
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

    // Admin'e tümünü, yöneticilere yönettiklerini, üyelere dahil oldukları projeleri döndürür.
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

    // Kimliğe göre projeyi arar; bulunamazsa null döndürür.
    public async Task<ProjectResponse?> GetByIdAsync(Guid id)
    {
        var project =
            await _unitOfWork.Projects.GetByIdAsync(id);

        return project is null
            ? null
            : _mapper.Map<ProjectResponse>(project);
    }

    // Proje isteğini entity'ye dönüştürür ve oluşturan kullanıcıyı yönetici olarak atar.
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

    // Var olan proje alanlarını istekten eşleyip güncelleme zamanını kaydeder.
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

    // Projeyi repository üzerinden siler; EF ilişkilerindeki cascade kuralları uygulanır.
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

    // Proje yöneticisi kimliğini istek sahibinin kimliğiyle karşılaştırır.
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

    // Aktif olup projede aktif üyeliği bulunmayan kullanıcıları seçer.
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
                    member.ProjectId == projectId &&
                    member.IsActive)
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

    // Üyelik kayıtlarını kullanıcı bilgileriyle zenginleştirerek yanıt listesine çevirir.
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

    // Kullanıcı/proje durumunu ve yinelenen üyeliği kontrol eder; pasif üyeliği yeniden açabilir.
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

        var existingMember =
            allMembers.FirstOrDefault(member =>
                member.ProjectId == projectId &&
                member.UserId == request.UserId);

        if (existingMember?.IsActive == true)
            throw new InvalidOperationException(
                "Kullanıcı zaten bu projeye eklenmiş.");

        var requestedMember = _mapper.Map<ProjectMember>(request);

        if (existingMember is not null)
        {
            existingMember.IsActive = true;
            existingMember.MemberRole = requestedMember.MemberRole;
            existingMember.JoinedDate = DateTime.UtcNow;
            existingMember.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.ProjectMembers.Update(existingMember);
            await _unitOfWork.SaveChangesAsync();
            return _mapper.Map<ProjectMemberResponse>(existingMember);
        }

        var member = requestedMember;

        member.ProjectId =
            projectId;

        member.JoinedDate =
            DateTime.UtcNow;

        await _unitOfWork.ProjectMembers.AddAsync(
            member);

        await _unitOfWork.SaveChangesAsync();

        return _mapper.Map<ProjectMemberResponse>(
            member);
    }

    // Yöneticiyi çıkarmaz; açık görevi olan üyeyi reddeder, aksi halde üyeliği pasifleştirir.
    public async Task<bool> RemoveMemberAsync(Guid projectId, Guid userId)
    {
        var project = await _unitOfWork.Projects.GetByIdAsync(projectId);
        if (project is null)
            return false;

        if (project.ManagerUserId == userId)
            throw new InvalidOperationException("Proje yöneticisi bu işlemle projeden çıkarılamaz.");

        var member = (await _unitOfWork.ProjectMembers.GetAllAsync())
            .FirstOrDefault(item => item.ProjectId == projectId && item.UserId == userId);
        if (member is null)
            return false;
        if (!member.IsActive)
            return true;

        var hasOpenTasks = (await _unitOfWork.Tasks.GetAllAsync())
            .Any(task => task.ProjectId == projectId &&
                         task.AssignedToUserId == userId &&
                         task.Status is not (TeamTaskManager.Entities.TaskStatus.Completed or TeamTaskManager.Entities.TaskStatus.Cancelled));
        if (hasOpenTasks)
            throw new InvalidOperationException(
                "Üyenin açık görevleri var. Üyeyi çıkarmadan önce bu görevleri başka bir aktif proje üyesine atayın.");

        member.IsActive = false;
        member.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.ProjectMembers.Update(member);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }
}
