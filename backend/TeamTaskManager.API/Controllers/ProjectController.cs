using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using TeamTaskManager.Business.Services;
using TeamTaskManager.DTO.Project;

namespace TeamTaskManager.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
// Proje ve üyelik işlemlerinde rolün yanında kaynak sahipliği/üyelik de kontrol edilir.
public class ProjectController : ControllerBase
{
    private readonly IProjectService _projectService;
    private readonly ITaskService _taskService;

    [ActivatorUtilitiesConstructor]
    public ProjectController(IProjectService projectService, ITaskService taskService)
    {
        _projectService = projectService;
        _taskService = taskService;
    }

    public ProjectController(IProjectService projectService) : this(projectService, null!) { }
// Rolüne göre kullanıcının erişebildiği proje listesini getirir.
[HttpGet]
public async Task<IActionResult> GetAll()
{
    var userId = GetCurrentUserId();

    if (userId is null)
    {
        return Unauthorized(
            "Kullanıcı kimliği JWT üzerinden alınamadı.");
    }

    var isAdmin =
        User.IsInRole("Admin");

    var isProjectManager =
        User.IsInRole("ProjectManager");

    var projects =
        await _projectService.GetAllAsync(
            userId.Value,
            isAdmin,
            isProjectManager);

    return Ok(projects);
}

  // Proje detayını Admin, yöneticisi veya aktif üyesi olan kullanıcıya açar.
  [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
{
    var userId = GetCurrentUserId();

    if (userId is null)
    {
        return Unauthorized(
            "Kullanıcı kimliği JWT üzerinden alınamadı.");
    }

    var project =
        await _projectService.GetByIdAsync(id);

    if (project is null)
    {
        return NotFound();
    }

    // Admin bütün projeleri görebilir.
    if (User.IsInRole("Admin"))
    {
        return Ok(project);
    }

    // ProjectManager sadece yönettiği projeyi görebilir.
    if (User.IsInRole("ProjectManager"))
    {
        var isManager =
            await _projectService.IsManagerAsync(
                id,
                userId.Value);

        if (!isManager)
        {
            return Forbid();
        }

        return Ok(project);
    }

    // TeamMember sadece üyesi olduğu projeyi görebilir.
    var members =
        await _projectService.GetMembersAsync(id);

    var isMember =
        members.Any(member =>
            member.UserId == userId.Value &&
            member.IsActive);

    if (!isMember)
    {
        return Forbid();
    }

    return Ok(project);
}

    // Yeni proje oluşturur; oluşturan kullanıcı proje yöneticisi yapılır.
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] ProjectCreateRequest request)
    {
        if (!User.IsInRole("Admin") && !User.IsInRole("ProjectManager"))
        {
            return Forbid();
        }

        var userId = GetCurrentUserId();

        if (userId is null)
        {
            return Unauthorized(
                "Kullanıcı kimliği JWT üzerinden alınamadı.");
        }

        var createdProject =
            await _projectService.CreateAsync(
                request,
                userId.Value);

        return CreatedAtAction(
            nameof(GetById),
            new { id = createdProject.Id },
            createdProject);
    }

    // Yalnızca Admin veya ilgili projenin yöneticisi proje bilgilerini günceller.
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] ProjectUpdateRequest request)
    {
        var userId = GetCurrentUserId();

        if (userId is null)
        {
            return Unauthorized(
                "Kullanıcı kimliği JWT üzerinden alınamadı.");
        }

        var project =
            await _projectService.GetByIdAsync(id);

        if (project is null)
        {
            return NotFound();
        }

        // Admin bütün projeleri güncelleyebilir.
        if (User.IsInRole("Admin"))
        {
            var updatedProject =
                await _projectService.UpdateAsync(
                    id,
                    request);

            return updatedProject is null
                ? NotFound()
                : Ok(updatedProject);
        }

        // Admin değilse sadece ProjectManager
        // kendi yönettiği projeyi güncelleyebilir.
        if (!User.IsInRole("ProjectManager"))
        {
            return Forbid();
        }

        var isManager =
            await _projectService.IsManagerAsync(
                id,
                userId.Value);

        if (!isManager)
        {
            return Forbid();
        }

        var result =
            await _projectService.UpdateAsync(
                id,
                request);

        return result is null
            ? NotFound()
            : Ok(result);
    }

    // Yetkili Admin/proje yöneticisi projeyi ve ilişkili verileri siler.
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var userId = GetCurrentUserId();

        if (userId is null)
        {
            return Unauthorized(
                "Kullanıcı kimliği JWT üzerinden alınamadı.");
        }

        var project =
            await _projectService.GetByIdAsync(id);

        if (project is null)
        {
            return NotFound();
        }

        // Admin bütün projeleri silebilir.
        if (User.IsInRole("Admin"))
        {
            var deleted =
                await _projectService.DeleteAsync(id);

            return deleted
                ? NoContent()
                : NotFound();
        }

        // Admin değilse sadece ProjectManager
        // kendi yönettiği projeyi silebilir.
        if (!User.IsInRole("ProjectManager"))
        {
            return Forbid();
        }

        var isManager =
            await _projectService.IsManagerAsync(
                id,
                userId.Value);

        if (!isManager)
        {
            return Forbid();
        }

        var result =
            await _projectService.DeleteAsync(id);

        return result
            ? NoContent()
            : NotFound();
    }
// Projeye eklenebilecek aktif kullanıcıları Admin veya proje yöneticisine listeler.
[HttpGet("{projectId:guid}/available-users")]
public async Task<IActionResult> GetAvailableUsers(
    Guid projectId)
{
    var userId = GetCurrentUserId();

    if (userId is null)
    {
        return Unauthorized(
            "Kullanıcı kimliği JWT üzerinden alınamadı.");
    }

    // Admin bütün projelerde mevcut kullanıcıları görebilir.
    if (User.IsInRole("Admin"))
    {
        var adminUsers =
            await _projectService.GetAvailableUsersAsync(
                projectId);

        return Ok(adminUsers);
    }

    // ProjectManager sadece yönettiği projenin
    // mevcut kullanıcılarını görebilir.
    if (User.IsInRole("ProjectManager"))
    {
        var isManager =
            await _projectService.IsManagerAsync(
                projectId,
                userId.Value);

        if (!isManager)
        {
            return Forbid();
        }

        var managerUsers =
            await _projectService.GetAvailableUsersAsync(
                projectId);

        return Ok(managerUsers);
    }

    // TeamMember kullanıcı listesine erişemez.
    return Forbid();
}

    // Üyelik listesini sadece projeye erişme yetkisi olan rollere döndürür.
    [HttpGet("{projectId:guid}/members")]
    public async Task<IActionResult> GetMembers(
        Guid projectId)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized(
                "Kullanıcı kimliği JWT üzerinden alınamadı.");
        }

        var project = await _projectService.GetByIdAsync(projectId);
        if (project is null)
        {
            return NotFound();
        }

        if (User.IsInRole("Admin"))
        {
            return Ok(await _projectService.GetMembersAsync(projectId));
        }

        if (User.IsInRole("ProjectManager"))
        {
            if (!await _projectService.IsManagerAsync(projectId, userId.Value))
            {
                return Forbid();
            }

            return Ok(await _projectService.GetMembersAsync(projectId));
        }

        var members =
            await _projectService.GetMembersAsync(
                projectId);

        return members.Any(member => member.UserId == userId.Value && member.IsActive)
            ? Ok(members)
            : Forbid();
    }

    // Raporu proje erişim kuralını uyguladıktan sonra görev servisinden alır.
    [HttpGet("{id:guid}/report")]
    public async Task<IActionResult> GetReport(Guid id)
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();
        var project = await _projectService.GetByIdAsync(id);
        if (project is null) return NotFound();
        if (!User.IsInRole("Admin"))
        {
            if (User.IsInRole("ProjectManager"))
            {
                if (!await _projectService.IsManagerAsync(id, userId.Value)) return Forbid();
            }
            else
            {
                var members = await _projectService.GetMembersAsync(id);
                if (!members.Any(member => member.UserId == userId.Value && member.IsActive)) return Forbid();
            }
        }
        var report = await _taskService.GetProjectReportAsync(id);
        return report is null ? NotFound() : Ok(report);
    }
// Üye ekleme yetkisi Admin ve yalnızca kendi projesini yöneten ProjectManager'a verilir.
[HttpPost("{projectId:guid}/members")]
public async Task<IActionResult> AddMember(
    Guid projectId,
    [FromBody] ProjectMemberRequest request)
{
    var userId = GetCurrentUserId();

    if (userId is null)
    {
        return Unauthorized(
            "Kullanıcı kimliği JWT üzerinden alınamadı.");
    }

    // Admin herhangi bir projeye üye ekleyebilir.
    if (User.IsInRole("Admin"))
    {
        var adminMember =
            await _projectService.AddMemberAsync(
                projectId,
                request);

        return Ok(adminMember);
    }

    // ProjectManager sadece yönettiği projeye
    // üye ekleyebilir.
    if (User.IsInRole("ProjectManager"))
    {
        var isManager =
            await _projectService.IsManagerAsync(
                projectId,
                userId.Value);

        if (!isManager)
        {
            return Forbid();
        }

        var managerMember =
            await _projectService.AddMemberAsync(
                projectId,
                request);

        return Ok(managerMember);
    }

    // TeamMember üye ekleyemez.
    return Forbid();
    }

    // Üye çıkarma yetkisini doğrular; açık görev varsa servis 400 yanıtına çevrilir.
    [HttpDelete("{projectId:guid}/members/{userId:guid}")]
    public async Task<IActionResult> RemoveMember(Guid projectId, Guid userId)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null)
            return Unauthorized();

        if (!User.IsInRole("Admin"))
        {
            if (!User.IsInRole("ProjectManager") ||
                !await _projectService.IsManagerAsync(projectId, currentUserId.Value))
            {
                return Forbid();
            }
        }

        try
        {
            return await _projectService.RemoveMemberAsync(projectId, userId)
                ? NoContent()
                : NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // İstek JWT'sindeki kullanıcı kimliğini GUID olarak döndürür.
    private Guid? GetCurrentUserId()
    {
        var userIdClaim =
            User.Claims.FirstOrDefault(c =>
                c.Type == "nameid" ||
                c.Type == ClaimTypes.NameIdentifier);

        if (userIdClaim is null)
        {
            return null;
        }

        return Guid.TryParse(
            userIdClaim.Value,
            out var userId)
            ? userId
            : null;
    }
}
