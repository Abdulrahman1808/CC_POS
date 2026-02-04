using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SuperAdminDashboard.Application.Features.Settings.DTOs;
using SuperAdminDashboard.Domain.Entities;
using SuperAdminDashboard.Domain.Exceptions;
using SuperAdminDashboard.Domain.Interfaces;
using SuperAdminDashboard.Infrastructure.Data;
using System.Text.Json;

namespace SuperAdminDashboard.API.Controllers;

/// <summary>
/// System settings endpoints
/// </summary>
[Authorize(Roles = "SuperAdmin")]
public class SettingsController : ApiControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IMapper _mapper;
    private readonly ILogger<SettingsController> _logger;

    public SettingsController(
        ApplicationDbContext context,
        ICurrentUserService currentUser,
        IMapper mapper,
        ILogger<SettingsController> logger)
    {
        _context = context;
        _currentUser = currentUser;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// Get all system settings
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetSettings([FromQuery] GetSettingsQuery query)
    {
        var queryable = _context.SystemSettings.AsQueryable();

        if (!string.IsNullOrEmpty(query.Category))
        {
            queryable = queryable.Where(s => s.Category == query.Category);
        }

        var settings = await queryable
            .OrderBy(s => s.Category)
            .ThenBy(s => s.Key)
            .ToListAsync();

        // Mask sensitive values
        var dtos = settings.Select(s => new SettingDto
        {
            Id = s.Id,
            Key = s.Key,
            Value = s.IsSensitive ? "********" : s.Value,
            Description = s.Description,
            Category = s.Category,
            IsSensitive = s.IsSensitive,
            UpdatedAt = s.UpdatedAt,
            UpdatedById = s.UpdatedById
        }).ToList();

        return Success(dtos);
    }

    /// <summary>
    /// Get a specific setting by key
    /// </summary>
    [HttpGet("{key}")]
    public async Task<IActionResult> GetSetting(string key)
    {
        var setting = await _context.SystemSettings
            .FirstOrDefaultAsync(s => s.Key == key);

        if (setting == null)
        {
            throw NotFoundException.ForSetting(key);
        }

        var dto = new SettingDto
        {
            Id = setting.Id,
            Key = setting.Key,
            Value = setting.IsSensitive ? "********" : setting.Value,
            Description = setting.Description,
            Category = setting.Category,
            IsSensitive = setting.IsSensitive,
            UpdatedAt = setting.UpdatedAt,
            UpdatedById = setting.UpdatedById
        };

        return Success(dto);
    }

    /// <summary>
    /// Create or update a setting
    /// </summary>
    [HttpPut("{key}")]
    public async Task<IActionResult> UpdateSetting(string key, [FromBody] UpdateSettingRequest request)
    {
        var setting = await _context.SystemSettings
            .FirstOrDefaultAsync(s => s.Key == key);

        var valueString = request.Value is JsonElement je 
            ? je.ValueKind == JsonValueKind.String 
                ? je.GetString() ?? ""
                : je.GetRawText()
            : request.Value?.ToString() ?? "";

        if (setting == null)
        {
            throw NotFoundException.ForSetting(key);
        }

        setting.Value = valueString;
        setting.UpdatedById = Guid.TryParse(_currentUser.UserId, out var userId) ? userId : null;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Setting updated: {Key}", key);

        return Success(_mapper.Map<SettingDto>(setting));
    }

    /// <summary>
    /// Create a new setting
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateSetting([FromBody] CreateSettingRequest request)
    {
        if (await _context.SystemSettings.AnyAsync(s => s.Key == request.Key))
        {
            throw new ConflictException($"Setting with key '{request.Key}' already exists");
        }

        var valueString = request.Value is JsonElement je 
            ? je.ValueKind == JsonValueKind.String 
                ? je.GetString() ?? ""
                : je.GetRawText()
            : request.Value?.ToString() ?? "";

        var setting = new SystemSetting
        {
            Key = request.Key,
            Value = valueString,
            Description = request.Description,
            Category = request.Category,
            IsSensitive = request.IsSensitive,
            UpdatedById = Guid.TryParse(_currentUser.UserId, out var userId) ? userId : null
        };

        _context.SystemSettings.Add(setting);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Setting created: {Key}", request.Key);

        return Created(_mapper.Map<SettingDto>(setting));
    }

    /// <summary>
    /// Delete a setting
    /// </summary>
    [HttpDelete("{key}")]
    public async Task<IActionResult> DeleteSetting(string key)
    {
        var setting = await _context.SystemSettings
            .FirstOrDefaultAsync(s => s.Key == key);

        if (setting == null)
        {
            throw NotFoundException.ForSetting(key);
        }

        _context.SystemSettings.Remove(setting);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Setting deleted: {Key}", key);

        return NoContent();
    }
}
