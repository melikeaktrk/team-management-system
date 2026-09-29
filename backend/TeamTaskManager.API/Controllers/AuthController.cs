using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamTaskManager.Business.Services;
using TeamTaskManager.DTO.Auth;

namespace TeamTaskManager.API.Controllers;

[ApiController]
[Route("api/[controller]")]
// Kayıt, giriş, oturum bilgisini görme ve parola değiştirme HTTP uçları.
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    // Yeni hesabı iş katmanında oluşturur; doğrulama hatasını 400 olarak döndürür.
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        try
        {
            var response = await _authService.RegisterAsync(request);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // E-posta/kullanıcı adı ve parolayla oturum açıp JWT yanıtı döndürür.
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        try
        {
            var response = await _authService.LoginAsync(request);
            return Ok(response);
        }
        catch (Exception ex) when (ex is InvalidOperationException or UnauthorizedAccessException)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // Geçerli token içindeki kullanıcı adı, e-posta ve rolleri döndürür.
    [Authorize]
    [HttpGet("me")]
    public IActionResult GetMe()
    {
        var userName = User.Identity?.Name;
        var email = User.FindFirst("email")?.Value
            ?? User.FindFirst(ClaimTypes.Email)?.Value
            ?? User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Email)?.Value;

        var roles = User.FindAll("role")
            .Select(c => c.Value)
            .Concat(User.FindAll(ClaimTypes.Role).Select(c => c.Value))
            .Distinct()
            .ToArray();

        return Ok(new
        {
            userName,
            email,
            roles,
            message = "Authenticated user"
        });
    }

    // Token sahibinin mevcut parolasını doğrulayıp yeni parolayı Identity'ye kaydeder.
    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        try
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("nameid");

            if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                return Unauthorized();

            await _authService.ChangePasswordAsync(userId, request.CurrentPassword, request.NewPassword);
            return Ok(new { message = "Şifre başarıyla değiştirildi." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
