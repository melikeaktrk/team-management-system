using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TeamTaskManager.DTO.User;
using TeamTaskManager.Entities;

namespace TeamTaskManager.Business.Services;

// Kullanıcı listeleme, oluşturma, profil güncelleme, durum ve rol işlemlerinin sözleşmesi.
public interface IUserService
{
    Task<IEnumerable<UserListResponse>> GetAllAsync();
    Task<UserDetailResponse?> GetByIdAsync(Guid id);
    Task<UserDetailResponse> CreateAsync(UserCreateRequest request);
    Task<UserDetailResponse?> UpdateAsync(Guid id, UserUpdateRequest request);
    Task<UserDetailResponse?> UpdateStatusAsync(Guid id, bool isActive);
    Task<UserDetailResponse?> UpdateRoleAsync(Guid id, string role);
}

// Kullanıcı/parola/rol yönetiminde ASP.NET Identity API'lerini kullanır.
public class UserService : IUserService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;
    private readonly IMapper _mapper;

    public UserService(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole<Guid>> roleManager,
        IMapper mapper)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _mapper = mapper;
    }

    // Kullanıcıları ad sırasıyla listeler ve Identity rollerini yanıta ekler.
    public async Task<IEnumerable<UserListResponse>> GetAllAsync()
    {
        var users = await _userManager.Users
            .OrderBy(u => u.UserName)
            .ToListAsync();

        var responses = new List<UserListResponse>();
        foreach (var user in users)
        {
            responses.Add(new UserListResponse
            {
                Id = user.Id,
                UserName = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                FirstName = user.FirstName,
                LastName = user.LastName,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                Roles = (await _userManager.GetRolesAsync(user)).ToList()
            });
        }

        return responses;
    }

    // Kimliğe göre kullanıcıyı ve atanmış rollerini detay DTO'sunda döndürür.
    public async Task<UserDetailResponse?> GetByIdAsync(Guid id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user is null)
            return null;

        return new UserDetailResponse
        {
            Id = user.Id,
            UserName = user.UserName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            FirstName = user.FirstName,
            LastName = user.LastName,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt,
            Roles = (await _userManager.GetRolesAsync(user)).ToList()
        };
    }

    // Alan ve benzersizlik kontrollerinden sonra hesabı açar ve seçilen rolü atar.
    public async Task<UserDetailResponse> CreateAsync(UserCreateRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.UserName))
            throw new InvalidOperationException("Kullanıcı adı zorunludur.");

        if (string.IsNullOrWhiteSpace(request.Email))
            throw new InvalidOperationException("E-posta zorunludur.");

        if (string.IsNullOrWhiteSpace(request.Password))
            throw new InvalidOperationException("Şifre zorunludur.");

        var role = string.IsNullOrWhiteSpace(request.Role) ? "TeamMember" : request.Role;
        if (!IsSupportedRole(role) || !await _roleManager.RoleExistsAsync(role))
            throw new InvalidOperationException("Geçersiz kullanıcı rolü.");

        var existsByEmail = await _userManager.FindByEmailAsync(request.Email);
        if (existsByEmail is not null)
            throw new InvalidOperationException("Bu e-posta ile kayıtlı kullanıcı mevcut.");

        var existsByUserName = await _userManager.FindByNameAsync(request.UserName);
        if (existsByUserName is not null)
            throw new InvalidOperationException("Bu kullanıcı adı kullanılmaktadır.");

        var user = new ApplicationUser
        {
            UserName = request.UserName,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException(errors);
        }

        var roleResult = await _userManager.AddToRoleAsync(user, role);
        if (!roleResult.Succeeded)
        {
            var errors = string.Join("; ", roleResult.Errors.Select(e => e.Description));
            await _userManager.DeleteAsync(user);
            throw new InvalidOperationException(errors);
        }

        return new UserDetailResponse
        {
            Id = user.Id,
            UserName = user.UserName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            FirstName = user.FirstName,
            LastName = user.LastName,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt,
            Roles = (await _userManager.GetRolesAsync(user)).ToList()
        };
    }

    // Çakışan e-posta/kullanıcı adını reddeder ve dolu profil alanlarını günceller.
    public async Task<UserDetailResponse?> UpdateAsync(Guid id, UserUpdateRequest request)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user is null)
            return null;

        if (!string.IsNullOrWhiteSpace(request.UserName) && request.UserName != user.UserName)
        {
            var existingByUserName = await _userManager.FindByNameAsync(request.UserName);
            if (existingByUserName is not null && existingByUserName.Id != user.Id)
                throw new InvalidOperationException("Bu kullanıcı adı kullanılmaktadır.");

            user.UserName = request.UserName;
        }

        if (!string.IsNullOrWhiteSpace(request.Email) && request.Email != user.Email)
        {
            var existingByEmail = await _userManager.FindByEmailAsync(request.Email);
            if (existingByEmail is not null && existingByEmail.Id != user.Id)
                throw new InvalidOperationException("Bu e-posta ile kayıtlı kullanıcı mevcut.");

            user.Email = request.Email;
            user.NormalizedEmail = request.Email;
            user.EmailConfirmed = true;
        }

        if (!string.IsNullOrWhiteSpace(request.FirstName))
            user.FirstName = request.FirstName;

        if (!string.IsNullOrWhiteSpace(request.LastName))
            user.LastName = request.LastName;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException(errors);
        }

        return await GetByIdAsync(user.Id);
    }

    // Hesabın aktiflik bayrağını Identity deposunda günceller.
    public async Task<UserDetailResponse?> UpdateStatusAsync(Guid id, bool isActive)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user is null)
            return null;

        user.IsActive = isActive;
        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException(errors);
        }

        return await GetByIdAsync(user.Id);
    }

    // Desteklenen tek rolü atar; son Admin hesabının Admin rolünden çıkarılmasını engeller.
    public async Task<UserDetailResponse?> UpdateRoleAsync(Guid id, string role)
    {
        if (!IsSupportedRole(role) || !await _roleManager.RoleExistsAsync(role))
            throw new InvalidOperationException("Geçersiz kullanıcı rolü.");

        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user is null) return null;

        var currentRoles = await _userManager.GetRolesAsync(user);
        if (currentRoles.Contains(role, StringComparer.OrdinalIgnoreCase))
            return await GetByIdAsync(id);

        if (currentRoles.Contains("Admin", StringComparer.OrdinalIgnoreCase))
        {
            var admins = await _userManager.GetUsersInRoleAsync("Admin");
            if (admins.Count <= 1)
                throw new InvalidOperationException("Son Admin kullanıcısının rolü değiştirilemez.");
        }

        var addResult = await _userManager.AddToRoleAsync(user, role);
        if (!addResult.Succeeded)
            throw new InvalidOperationException(string.Join("; ", addResult.Errors.Select(e => e.Description)));

        var rolesToRemove = currentRoles
            .Where(currentRole => !string.Equals(currentRole, role, StringComparison.OrdinalIgnoreCase))
            .ToArray();
        if (rolesToRemove.Length > 0)
        {
            var removeResult = await _userManager.RemoveFromRolesAsync(user, rolesToRemove);
            if (!removeResult.Succeeded)
                throw new InvalidOperationException(string.Join("; ", removeResult.Errors.Select(e => e.Description)));
        }

        return await GetByIdAsync(id);
    }

    // Uygulamanın kullandığı üç rol adını izin listesi olarak tutar.
    private static bool IsSupportedRole(string role) =>
        new[] { "Admin", "ProjectManager", "TeamMember" }
            .Contains(role, StringComparer.OrdinalIgnoreCase);
}
