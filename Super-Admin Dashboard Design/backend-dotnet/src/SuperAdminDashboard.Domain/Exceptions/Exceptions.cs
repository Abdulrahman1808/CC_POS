namespace SuperAdminDashboard.Domain.Exceptions;

/// <summary>
/// Base application exception
/// </summary>
public class AppException : Exception
{
    public string Code { get; }
    public int StatusCode { get; }

    public AppException(string message, string code = "APP_ERROR", int statusCode = 500)
        : base(message)
    {
        Code = code;
        StatusCode = statusCode;
    }
}

/// <summary>
/// Resource not found exception
/// </summary>
public class NotFoundException : AppException
{
    public string ResourceType { get; }
    public string? ResourceId { get; }

    public NotFoundException(string resourceType, string? resourceId = null)
        : base(
            resourceId != null 
                ? $"{resourceType} with ID '{resourceId}' was not found" 
                : $"{resourceType} was not found",
            "NOT_FOUND",
            404)
    {
        ResourceType = resourceType;
        ResourceId = resourceId;
    }

    public static NotFoundException ForUser(Guid id) => new("User", id.ToString());
    public static NotFoundException ForTenant(Guid id) => new("Tenant", id.ToString());
    public static NotFoundException ForPlan(Guid id) => new("Plan", id.ToString());
    public static NotFoundException ForSetting(string key) => new("Setting", key);
}

/// <summary>
/// Validation exception with multiple errors
/// </summary>
public class ValidationException : AppException
{
    public IDictionary<string, string[]> Errors { get; }

    public ValidationException(IDictionary<string, string[]> errors)
        : base("One or more validation errors occurred", "VALIDATION_ERROR", 400)
    {
        Errors = errors;
    }

    public ValidationException(string field, string message)
        : this(new Dictionary<string, string[]> { { field, new[] { message } } })
    {
    }
}

/// <summary>
/// Authentication exception
/// </summary>
public class UnauthorizedException : AppException
{
    public UnauthorizedException(string message = "Authentication required")
        : base(message, "UNAUTHORIZED", 401)
    {
    }

    public static UnauthorizedException InvalidCredentials() => 
        new("Invalid email or password");
    
    public static UnauthorizedException TokenExpired() => 
        new("Token has expired");
    
    public static UnauthorizedException TokenInvalid() => 
        new("Invalid token");
    
    public static UnauthorizedException AccountLocked() => 
        new("Account is locked due to too many failed attempts");
    
    public static UnauthorizedException AccountInactive() => 
        new("Account is inactive");
    
    public static UnauthorizedException MfaRequired() => 
        new("MFA verification required");
}

/// <summary>
/// Forbidden exception (authorized but not permitted)
/// </summary>
public class ForbiddenException : AppException
{
    public ForbiddenException(string message = "You don't have permission to perform this action")
        : base(message, "FORBIDDEN", 403)
    {
    }
}

/// <summary>
/// Conflict exception (duplicate resource)
/// </summary>
public class ConflictException : AppException
{
    public ConflictException(string message)
        : base(message, "CONFLICT", 409)
    {
    }

    public static ConflictException EmailExists(string email) => 
        new($"Email '{email}' is already registered");
    
    public static ConflictException SlugExists(string slug) => 
        new($"Slug '{slug}' is already taken");
    
    public static ConflictException DomainExists(string domain) => 
        new($"Domain '{domain}' is already registered");
}
