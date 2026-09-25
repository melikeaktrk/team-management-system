using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using TeamTaskManager.DataAccess;
using TeamTaskManager.DTO.Auth;
using TeamTaskManager.Entities;

namespace TeamTaskManager.Business.Services;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request);
    Task<AuthResponse> LoginAsync(LoginRequest request);
    Task<bool> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword);
}

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IConfiguration _configuration;
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IConfiguration configuration,
        IMapper mapper,
        IUnitOfWork unitOfWork)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _configuration = configuration;
        _mapper = mapper;
        _unitOfWork = unitOfWork;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser is not null)
            throw new InvalidOperationException("Bu e-posta ile kayıtlı kullanıcı mevcut.");

        var user = _mapper.Map<ApplicationUser>(request);
        user.Email = request.Email;
        user.UserName = string.IsNullOrWhiteSpace(request.UserName) ? request.Email : request.UserName;

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException(errors);
        }

        var token = await GenerateJwtTokenAsync(user);
        return new AuthResponse
        {
            Token = token,
            ExpiresAt = DateTime.UtcNow.AddHours(2),
            UserName = user.UserName,
            Email = user.Email!
        };
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var loginKey = string.IsNullOrWhiteSpace(request.Email)
            ? request.UserName
            : request.Email;

        if (string.IsNullOrWhiteSpace(loginKey))
            throw new InvalidOperationException("E-posta veya kullanıcı adı gerekli.");

        var user = await _userManager.Users.FirstOrDefaultAsync(u =>
            u.Email == loginKey || u.UserName == loginKey);

        if (user is null)
            throw new InvalidOperationException("Kullanıcı bulunamadı.");

        if (!user.IsActive)
            throw new UnauthorizedAccessException("Hesabınız pasif edildiği için giriş yapamazsınız.");

        var isValid = await _signInManager.CheckPasswordSignInAsync(user, request.Password, false);
        if (!isValid.Succeeded)
            throw new UnauthorizedAccessException("E-posta veya şifre hatalı.");

        var token = await GenerateJwtTokenAsync(user);
        return new AuthResponse
        {
            Token = token,
            ExpiresAt = DateTime.UtcNow.AddHours(2),
            UserName = user.UserName ?? string.Empty,
            Email = user.Email ?? string.Empty
        };
    }

    public async Task<bool> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword)
    {
        if (string.IsNullOrWhiteSpace(currentPassword) || string.IsNullOrWhiteSpace(newPassword))
            throw new InvalidOperationException("Mevcut şifre ve yeni şifre zorunludur.");

        if (newPassword.Length < 8)
            throw new InvalidOperationException("Yeni şifre en az 8 karakter olmalıdır.");

        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            throw new InvalidOperationException("Kullanıcı bulunamadı.");

        var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException(errors);
        }

        return true;
    }

    private async Task<string> GenerateJwtTokenAsync(ApplicationUser user)
    {
        var key = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] ?? "TeamTaskManagerVeryStrongSecretKey1234567890");
        var issuer = _configuration["Jwt:Issuer"] ?? "TeamTaskManagerAPI";
        var audience = _configuration["Jwt:Audience"] ?? "TeamTaskManagerClient";

        var roles = (await _userManager.GetRolesAsync(user))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.UserName ?? string.Empty)
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim("role", role));
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(2),
            Issuer = issuer,
            Audience = audience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var securityToken = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(securityToken);
    }
}
