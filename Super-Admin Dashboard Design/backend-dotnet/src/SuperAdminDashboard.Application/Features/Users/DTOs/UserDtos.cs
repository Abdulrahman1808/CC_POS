using SuperAdminDashboard.Domain.Enums;

namespace SuperAdminDashboard.Application.Features.Users.DTOs;

// ============================================
// Request DTOs
// ============================================

public record CreateUserRequest
{
    public required string Email { get; init; }
    public required string Password { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public UserRole Role { get; init; } = UserRole.Viewer;
}

public record UpdateUserRequest
{
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public UserRole? Role { get; init; }
    public string? AvatarUrl { get; init; }
}

public record UpdateUserStatusRequest
{
    public required UserStatus Status { get; init; }
}

public record GetUsersQuery
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public UserRole? Role { get; init; }
    public UserStatus? Status { get; init; }
    public string? Search { get; init; }
}

// ============================================
// Response DTOs
// ============================================

public record UserListItemDto
{
    public Guid Id { get; init; }
    public required string Email { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public UserRole Role { get; init; }
    public UserStatus Status { get; init; }
    public string? AvatarUrl { get; init; }
    public bool MfaEnabled { get; init; }
    public DateTime? LastLoginAt { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record UserDetailDto : UserListItemDto
{
    public DateTime? EmailVerifiedAt { get; init; }
    public string? LastLoginIp { get; init; }
    public int FailedLoginAttempts { get; init; }
    public DateTime UpdatedAt { get; init; }
}
