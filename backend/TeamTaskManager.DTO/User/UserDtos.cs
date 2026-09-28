using System.ComponentModel.DataAnnotations;

namespace TeamTaskManager.DTO.User;

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

public class UserStatusUpdateRequest
{
    public bool IsActive { get; set; }
}

public class UserRoleUpdateRequest
{
    [Required]
    [RegularExpression("^(Admin|ProjectManager|TeamMember)$", ErrorMessage = "Geçersiz kullanıcı rolü.")]
    public string Role { get; set; } = string.Empty;
}
