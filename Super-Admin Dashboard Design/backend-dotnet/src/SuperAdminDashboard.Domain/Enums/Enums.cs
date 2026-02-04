namespace SuperAdminDashboard.Domain.Enums;

public enum UserRole
{
    SuperAdmin = 1,
    Admin = 2,
    Viewer = 3
}

public enum UserStatus
{
    Active = 1,
    Inactive = 2,
    Locked = 3
}

public enum TenantStatus
{
    Pending = 1,
    Active = 2,
    Suspended = 3,
    Deleted = 4
}

public enum SubscriptionStatus
{
    Active = 1,
    Cancelled = 2,
    Expired = 3,
    PastDue = 4
}

public enum AuditAction
{
    Create = 1,
    Read = 2,
    Update = 3,
    Delete = 4,
    Login = 5,
    Logout = 6,
    LoginFailed = 7,
    PasswordReset = 8,
    MfaEnabled = 9,
    MfaDisabled = 10,
    SettingsChanged = 11
}
