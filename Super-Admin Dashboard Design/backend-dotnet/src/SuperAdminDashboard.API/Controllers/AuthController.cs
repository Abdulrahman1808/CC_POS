using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SuperAdminDashboard.Application.Features.Auth.DTOs;
using SuperAdminDashboard.Domain.Entities;
using SuperAdminDashboard.Domain.Enums;
using SuperAdminDashboard.Domain.Exceptions;
using SuperAdminDashboard.Domain.Interfaces;
using SuperAdminDashboard.Infrastructure.Data;
using SuperAdminDashboard.Infrastructure.Services;

namespace SuperAdminDashboard.API.Controllers;

/// <summary>
/// Authentication endpoints
/// </summary>
[AllowAnonymous]
public class AuthController : ApiControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IJwtService _jwtService;
    private readonly IPasswordService _passwordService;
    private readonly IMapper _mapper;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        ApplicationDbContext context,
        IJwtService jwtService,
        IPasswordService passwordService,
        IMapper mapper,
        ILogger<AuthController> logger)
    {
        _context = context;
        _jwtService = jwtService;
        _passwordService = passwordService;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// Login with email and password
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), 200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower());

        if (user == null)
        {
            _logger.LogWarning("Login failed: User not found - {Email}", request.Email);
            throw UnauthorizedException.InvalidCredentials();
        }

        // Check if account is locked
        if (user.IsLocked)
        {
            _logger.LogWarning("Login failed: Account locked - {Email}", request.Email);
            throw UnauthorizedException.AccountLocked();
        }

        // Check if account is active
        if (user.Status != UserStatus.Active)
        {
            throw UnauthorizedException.AccountInactive();
        }

        // Verify password
        if (!_passwordService.VerifyPassword(request.Password, user.PasswordHash))
        {
            user.FailedLoginAttempts++;
            
            // Lock account after 5 failed attempts
            if (user.FailedLoginAttempts >= 5)
            {
                user.LockedUntil = DateTime.UtcNow.AddMinutes(30);
                _logger.LogWarning("Account locked due to failed attempts - {Email}", request.Email);
            }
            
            await _context.SaveChangesAsync();
            throw UnauthorizedException.InvalidCredentials();
        }

        // Check if MFA is required
        if (user.MfaEnabled)
        {
            // Generate temporary token for MFA flow
            var tempToken = Guid.NewGuid().ToString();
            // In production, store this in Redis with short TTL
            return Ok(new MfaRequiredResponse { TempToken = tempToken });
        }

        // Reset failed attempts and update login info
        user.FailedLoginAttempts = 0;
        user.LockedUntil = null;
        user.LastLoginAt = DateTime.UtcNow;
        user.LastLoginIp = HttpContext.Connection.RemoteIpAddress?.ToString();

        // Create session
        var refreshToken = _jwtService.GenerateRefreshToken();
        var session = new Session
        {
            UserId = user.Id,
            RefreshTokenHash = _jwtService.HashRefreshToken(refreshToken),
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent = Request.Headers.UserAgent.ToString(),
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        _context.Sessions.Add(session);
        await _context.SaveChangesAsync();

        var accessToken = _jwtService.GenerateAccessToken(user, session.Id.ToString());

        _logger.LogInformation("User logged in successfully - {Email}", request.Email);

        return Success(new LoginResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            User = _mapper.Map<UserDto>(user)
        });
    }

    /// <summary>
    /// Logout and invalidate refresh token
    /// </summary>
    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var sessionIdClaim = User.FindFirst("session_id")?.Value;
        
        if (!string.IsNullOrEmpty(sessionIdClaim) && Guid.TryParse(sessionIdClaim, out var sessionId))
        {
            var session = await _context.Sessions.FindAsync(sessionId);
            if (session != null)
            {
                session.RevokedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        return Success<object?>(null, "Logged out successfully");
    }

    /// <summary>
    /// Refresh access token
    /// </summary>
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request)
    {
        var refreshTokenHash = _jwtService.HashRefreshToken(request.RefreshToken);
        
        var session = await _context.Sessions
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => 
                s.RefreshTokenHash == refreshTokenHash &&
                s.RevokedAt == null &&
                s.ExpiresAt > DateTime.UtcNow);

        if (session == null)
        {
            throw UnauthorizedException.TokenInvalid();
        }

        if (session.User.Status != UserStatus.Active)
        {
            throw UnauthorizedException.AccountInactive();
        }

        // Generate new tokens
        var newRefreshToken = _jwtService.GenerateRefreshToken();
        session.RefreshTokenHash = _jwtService.HashRefreshToken(newRefreshToken);
        session.UpdatedAt = DateTime.UtcNow;

        var accessToken = _jwtService.GenerateAccessToken(session.User, session.Id.ToString());

        await _context.SaveChangesAsync();

        return Success(new TokenResponse
        {
            AccessToken = accessToken,
            RefreshToken = newRefreshToken
        });
    }

    /// <summary>
    /// Get current user profile
    /// </summary>
    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw UnauthorizedException.TokenInvalid();
        }

        var user = await _context.Users.FindAsync(userId);
        
        if (user == null)
        {
            throw NotFoundException.ForUser(userId);
        }

        return Success(_mapper.Map<CurrentUserDto>(user));
    }
}
