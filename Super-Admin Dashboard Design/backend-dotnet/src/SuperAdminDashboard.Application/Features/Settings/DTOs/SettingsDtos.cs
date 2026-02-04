namespace SuperAdminDashboard.Application.Features.Settings.DTOs;

// ============================================
// Request DTOs
// ============================================

public record CreateSettingRequest
{
    public required string Key { get; init; }
    public required object Value { get; init; }
    public string? Description { get; init; }
    public string Category { get; init; } = "general";
    public bool IsSensitive { get; init; }
}

public record UpdateSettingRequest
{
    public required object Value { get; init; }
}

public record GetSettingsQuery
{
    public string? Category { get; init; }
}

// ============================================
// Response DTOs
// ============================================

public record SettingDto
{
    public Guid Id { get; init; }
    public required string Key { get; init; }
    public required string Value { get; init; }
    public string? Description { get; init; }
    public required string Category { get; init; }
    public bool IsSensitive { get; init; }
    public DateTime UpdatedAt { get; init; }
    public Guid? UpdatedById { get; init; }
}
