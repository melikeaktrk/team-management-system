using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TeamTaskManager.DTO.User;
using TeamTaskManager.Entities;

namespace TeamTaskManager.Business.Services;

public interface IUserService
{
    Task<IEnumerable<UserListResponse>> GetAllAsync();
    Task<UserDetailResponse?> GetByIdAsync(Guid id);
    Task<UserDetailResponse> CreateAsync(UserCreateRequest request);
    Task<UserDetailResponse?> UpdateAsync(Guid id, UserUpdateRequest request);
    Task<UserDetailResponse?> UpdateStatusAsync(Guid id, bool isActive);
}

public class UserService : IUserService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IMapper _mapper;

    public UserService(UserManager<ApplicationUser> userManager, IMapper mapper)
    {
        _userManager = userManager;
        _mapper = mapper;
    }

    public async Task<IEnumerable<UserListResponse>> GetAllAsync()
    {
        var users = await _userManager.Users
            .OrderBy(u => u.UserName)
            .ToListAsync();

        return users.Select(u => new UserListResponse
        {
            Id = u.Id,
            UserName = u.UserName ?? string.Empty,
            Email = u.Email ?? string.Empty,
            FirstName = u.FirstName,
            LastName = u.LastName,
            IsActive = u.IsActive,
            CreatedAt = u.CreatedAt
        });
    }

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
            LastLoginAt = user.LastLoginAt
        };
    }

    public async Task<UserDetailResponse> CreateAsync(UserCreateRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.UserName))
            throw new InvalidOperationException("Kullanıcı adı zorunludur.");

        if (string.IsNullOrWhiteSpace(request.Email))
            throw new InvalidOperationException("E-posta zorunludur.");

        if (string.IsNullOrWhiteSpace(request.Password))
            throw new InvalidOperationException("Şifre zorunludur.");

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

        return new UserDetailResponse
        {
            Id = user.Id,
            UserName = user.UserName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            FirstName = user.FirstName,
            LastName = user.LastName,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt
        };
    }

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
}
