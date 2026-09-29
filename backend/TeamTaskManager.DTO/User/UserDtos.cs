using System.ComponentModel.DataAnnotations;

namespace TeamTaskManager.DTO.User;

// Kullanıcı tablolarında kullanılan kısa profil ve rol yanıtı.
public class UserListResponse
{
    public Guid Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<string> Roles { get; set; } = [];
}

// Kullanıcı ayrıntı ekranı için liste yanıtına son giriş zamanını ekler.
public class UserDetailResponse
{
    public Guid Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public List<string> Roles { get; set; } = [];
}

// Admin'in hesap oluştururken gönderdiği, alan doğrulamalı istek modeli.
public class UserCreateRequest
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
    [StringLength(100)]
    public string? FirstName { get; set; }
    [StringLength(100)]
    public string? LastName { get; set; }
    [RegularExpression("^(Admin|ProjectManager|TeamMember)$", ErrorMessage = "Geçersiz kullanıcı rolü.")]
    public string? Role { get; set; }
}

// Kullanıcı profilinin değiştirilebilir temel alanları; null alanlar korunur.
public class UserUpdateRequest
{
    [StringLength(100, MinimumLength = 1)]
    public string? UserName { get; set; }
    [EmailAddress]
    [StringLength(255)]
    public string? Email { get; set; }
    [StringLength(100)]
    public string? FirstName { get; set; }
    [StringLength(100)]
    public string? LastName { get; set; }
}

// Hesap erişimini etkin/pasif yapmak için kullanılan tek alanlı istek.
public class UserStatusUpdateRequest
{
    public bool IsActive { get; set; }
}

// Kullanıcıya atanacak uygulama rolü; izinli rol adlarıyla doğrulanır.
public class UserRoleUpdateRequest
{
    [Required]
    [RegularExpression("^(Admin|ProjectManager|TeamMember)$", ErrorMessage = "Geçersiz kullanıcı rolü.")]
    public string Role { get; set; } = string.Empty;
}
