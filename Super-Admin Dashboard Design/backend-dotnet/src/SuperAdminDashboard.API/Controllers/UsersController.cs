using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SuperAdminDashboard.Application.Features.Users.DTOs;
using SuperAdminDashboard.Domain.Entities;
using SuperAdminDashboard.Domain.Enums;
using SuperAdminDashboard.Domain.Exceptions;
using SuperAdminDashboard.Domain.Interfaces;
using SuperAdminDashboard.Infrastructure.Data;
using SuperAdminDashboard.Infrastructure.Services;

namespace SuperAdminDashboard.API.Controllers;

/// <summary>
/// User management endpoints (Super Admin only)
/// </summary>
[Authorize(Roles = "SuperAdmin")]
public class UsersController : ApiControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IPasswordService _passwordService;
    private readonly ICurrentUserService _currentUser;
    private readonly IMapper _mapper;
    private readonly ILogger<UsersController> _logger;

    public UsersController(
        ApplicationDbContext context,
        IPasswordService passwordService,
        ICurrentUserService currentUser,
        IMapper mapper,
        ILogger<UsersController> logger)
    {
        _context = context;
        _passwordService = passwordService;
        _currentUser = currentUser;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// Get all admin users with filtering
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetUsers([FromQuery] GetUsersQuery query)
    {
        var queryable = _context.Users.AsQueryable();

        // Filter by role
        if (query.Role.HasValue)
        {
            queryable = queryable.Where(u => u.Role == query.Role.Value);
        }

        // Filter by status
        if (query.Status.HasValue)
        {
            queryable = queryable.Where(u => u.Status == query.Status.Value);
        }

        // Search
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.ToLower();
            queryable = queryable.Where(u => 
                u.Email.ToLower().Contains(search) ||
                (u.FirstName != null && u.FirstName.ToLower().Contains(search)) ||
                (u.LastName != null && u.LastName.ToLower().Contains(search)));
        }

        var totalCount = await queryable.CountAsync();

        var users = await queryable
            .OrderByDescending(u => u.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        return SuccessPaginated(
            _mapper.Map<List<UserListItemDto>>(users),
            query.Page,
            query.PageSize,
            totalCount);
    }

    /// <summary>
    /// Get user by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetUser(Guid id)
    {
        var user = await _context.Users.FindAsync(id);

        if (user == null)
        {
            throw NotFoundException.ForUser(id);
        }

        return Success(_mapper.Map<UserDetailDto>(user));
    }

    /// <summary>
    /// Create a new admin user
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
    {
        // Check for duplicate email
        if (await _context.Users.AnyAsync(u => u.Email.ToLower() == request.Email.ToLower()))
        {
            throw ConflictException.EmailExists(request.Email);
        }

        var user = new User
        {
            Email = request.Email.ToLower(),
            PasswordHash = _passwordService.HashPassword(request.Password),
            FirstName = request.FirstName,
            LastName = request.LastName,
            Role = request.Role,
            Status = UserStatus.Active
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        _logger.LogInformation("User created: {UserId} - {Email}", user.Id, user.Email);

        return Created(_mapper.Map<UserListItemDto>(user));
    }

    /// <summary>
    /// Update a user
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UpdateUserRequest request)
    {
        var user = await _context.Users.FindAsync(id);

        if (user == null)
        {
            throw NotFoundException.ForUser(id);
        }

        // Update fields
        if (request.FirstName != null) user.FirstName = request.FirstName;
        if (request.LastName != null) user.LastName = request.LastName;
        if (request.Role.HasValue) user.Role = request.Role.Value;
        if (request.AvatarUrl != null) user.AvatarUrl = request.AvatarUrl;

        await _context.SaveChangesAsync();

        _logger.LogInformation("User updated: {UserId}", id);

        return Success(_mapper.Map<UserDetailDto>(user));
    }

    /// <summary>
    /// Update user status
    /// </summary>
    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> UpdateUserStatus(Guid id, [FromBody] UpdateUserStatusRequest request)
    {
        // Prevent self-status change
        if (_currentUser.UserId == id.ToString())
        {
            throw new ForbiddenException("Cannot change your own status");
        }

        var user = await _context.Users.FindAsync(id);

        if (user == null)
        {
            throw NotFoundException.ForUser(id);
        }

        user.Status = request.Status;
        
        // Clear lock if activating
        if (request.Status == UserStatus.Active)
        {
            user.LockedUntil = null;
            user.FailedLoginAttempts = 0;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("User status updated: {UserId} - {Status}", id, request.Status);

        return Success(_mapper.Map<UserDetailDto>(user));
    }

    /// <summary>
    /// Delete a user
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteUser(Guid id)
    {
        // Prevent self-deletion
        if (_currentUser.UserId == id.ToString())
        {
            throw new ForbiddenException("Cannot delete your own account");
        }

        var user = await _context.Users.FindAsync(id);

        if (user == null)
        {
            throw NotFoundException.ForUser(id);
        }

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();

        _logger.LogInformation("User deleted: {UserId}", id);

        return NoContent();
    }
}
