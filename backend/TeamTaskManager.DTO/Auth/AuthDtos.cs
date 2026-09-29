using System.ComponentModel.DataAnnotations;

namespace TeamTaskManager.DTO.Auth;

// Kayıt endpoint'inin aldığı alanlar; Data Annotations API doğrulamasını çalıştırır.
public class RegisterRequest
{
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string UserName { get; set; } = string.Empty;
    [Required]
    [EmailAddress]
    [StringLength(255)]
    public string Email { get; set; } = string.Empty;
    [Required]
    [StringLength(128, MinimumLength = 8)]
    public string Password { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
}

// Girişte e-posta veya kullanıcı adından en az birini zorunlu tutar.
public class LoginRequest : IValidatableObject
{
    [StringLength(100)]
    public string? UserName { get; set; }
    [EmailAddress]
    [StringLength(255)]
    public string? Email { get; set; }
    [Required]
    [StringLength(128, MinimumLength = 1)]
    public string Password { get; set; } = string.Empty;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(Email) && string.IsNullOrWhiteSpace(UserName))
            yield return new ValidationResult("E-posta veya kullanıcı adı gerekli.", [nameof(Email), nameof(UserName)]);
    }
}

// Başarılı giriş/kayıt sonrası istemciye verilen token ve kullanıcı özetidir.
public class AuthResponse
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

// Parola değiştirme endpoint'inin doğrulanan istek gövdesidir.
public class ChangePasswordRequest
{
    [Required]
    public string CurrentPassword { get; set; } = string.Empty;
    [Required]
    [StringLength(128, MinimumLength = 8)]
    public string NewPassword { get; set; } = string.Empty;
}
