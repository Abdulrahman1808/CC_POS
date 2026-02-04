using SuperAdminDashboard.Domain.Enums;

namespace SuperAdminDashboard.Application.Features.Auth.DTOs;

// ============================================
// Request DTOs
// ============================================

public record LoginRequest
{
    public required string Email { get; init; }
    public required string Password { get; init; }
}

public record RefreshTokenRequest
{
    public required string RefreshToken { get; init; }
}

public record ForgotPasswordRequest
{
    public required string Email { get; init; }
}

public record ResetPasswordRequest
{
    public required string Token { get; init; }
    public required string Password { get; init; }
}

public record MfaVerifyRequest
{
    public required string TempToken { get; init; }
    public required string Code { get; init; }
}

// ============================================
// Response DTOs
// ============================================

public record LoginResponse
{
    public required string AccessToken { get; init; }
    public required string RefreshToken { get; init; }
    public required UserDto User { get; init; }
}

public record MfaRequiredResponse
{
    public bool MfaRequired { get; init; } = true;
    public required string TempToken { get; init; }
}

public record TokenResponse
{
    public required string AccessToken { get; init; }
    public required string RefreshToken { get; init; }
}

public record MfaSetupResponse
{
    public required string Secret { get; init; }
    public required string OtpauthUrl { get; init; }
}

// ============================================
// User DTOs
// ============================================

public record UserDto
{
    public Guid Id { get; init; }
    public required string Email { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? FullName { get; init; }
    public UserRole Role { get; init; }
    public string? AvatarUrl { get; init; }
}

public record CurrentUserDto
{
    public Guid Id { get; init; }
    public required string Email { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public UserRole Role { get; init; }
    public UserStatus Status { get; init; }
    public string? AvatarUrl { get; init; }
    public bool MfaEnabled { get; init; }
    public DateTime? EmailVerifiedAt { get; init; }
    public DateTime? LastLoginAt { get; init; }
    public DateTime CreatedAt { get; init; }
}
