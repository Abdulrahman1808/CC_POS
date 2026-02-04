using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MediatR;
using SuperAdminDashboard.Application.Features.Tenants.DTOs;
using SuperAdminDashboard.Domain.Entities;
using SuperAdminDashboard.Domain.Enums;
using SuperAdminDashboard.Domain.Events;
using SuperAdminDashboard.Domain.Exceptions;
using SuperAdminDashboard.Infrastructure.Data;
using System.Text.Json;

namespace SuperAdminDashboard.API.Controllers;

/// <summary>
/// Tenant management endpoints
/// </summary>
[Authorize(Roles = "SuperAdmin,Admin")]
public class TenantsController : ApiControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IMediator _mediator;
    private readonly ILogger<TenantsController> _logger;

    public TenantsController(
        ApplicationDbContext context,
        IMapper mapper,
        IMediator mediator,
        ILogger<TenantsController> logger)
    {
        _context = context;
        _mapper = mapper;
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Get all tenants with pagination
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetTenants([FromQuery] GetTenantsQuery query)
    {
        var queryable = _context.Tenants
            .Include(t => t.Plan)
            .AsQueryable();

        // Filter by status
        if (query.Status.HasValue)
        {
            queryable = queryable.Where(t => t.Status == query.Status.Value);
        }

        // Search
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.ToLower();
            queryable = queryable.Where(t => 
                t.Name.ToLower().Contains(search) ||
                t.Slug.ToLower().Contains(search) ||
                (t.ContactEmail != null && t.ContactEmail.ToLower().Contains(search)));
        }

        // Get total count
        var totalCount = await queryable.CountAsync();

        // Sorting
        queryable = query.SortBy?.ToLower() switch
        {
            "name" => query.SortDescending 
                ? queryable.OrderByDescending(t => t.Name) 
                : queryable.OrderBy(t => t.Name),
            "status" => query.SortDescending 
                ? queryable.OrderByDescending(t => t.Status) 
                : queryable.OrderBy(t => t.Status),
            _ => query.SortDescending 
                ? queryable.OrderByDescending(t => t.CreatedAt) 
                : queryable.OrderBy(t => t.CreatedAt)
        };

        // Pagination
        var tenants = await queryable
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        return SuccessPaginated(
            _mapper.Map<List<TenantDto>>(tenants),
            query.Page,
            query.PageSize,
            totalCount);
    }

    /// <summary>
    /// Get tenant by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetTenant(Guid id)
    {
        var tenant = await _context.Tenants
            .Include(t => t.Plan)
            .Include(t => t.Customers)
            .Include(t => t.Subscriptions)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (tenant == null)
        {
            throw NotFoundException.ForTenant(id);
        }

        return Success(_mapper.Map<TenantDetailDto>(tenant));
    }

    /// <summary>
    /// Create a new tenant
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> CreateTenant([FromBody] CreateTenantRequest request)
    {
        // Check for duplicate slug
        if (await _context.Tenants.AnyAsync(t => t.Slug == request.Slug))
        {
            throw ConflictException.SlugExists(request.Slug);
        }

        // Check for duplicate domain
        if (!string.IsNullOrEmpty(request.Domain) && 
            await _context.Tenants.AnyAsync(t => t.Domain == request.Domain))
        {
            throw ConflictException.DomainExists(request.Domain);
        }

        var tenant = new Tenant
        {
            Name = request.Name,
            Slug = request.Slug.ToLower(),
            Domain = request.Domain,
            LogoUrl = request.LogoUrl,
            ContactEmail = request.ContactEmail,
            ContactPhone = request.ContactPhone,
            PlanId = request.PlanId,
            Status = TenantStatus.Pending,
            Settings = request.Settings != null 
                ? JsonSerializer.Serialize(request.Settings) 
                : null
        };

        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Tenant created: {TenantId} - {TenantName}", tenant.Id, tenant.Name);

        return Created(_mapper.Map<TenantDto>(tenant));
    }

    /// <summary>
    /// Update a tenant
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> UpdateTenant(Guid id, [FromBody] UpdateTenantRequest request)
    {
        var tenant = await _context.Tenants.FindAsync(id);
        
        if (tenant == null)
        {
            throw NotFoundException.ForTenant(id);
        }

        // Check for duplicate slug if changed
        if (!string.IsNullOrEmpty(request.Slug) && request.Slug != tenant.Slug)
        {
            if (await _context.Tenants.AnyAsync(t => t.Slug == request.Slug && t.Id != id))
            {
                throw ConflictException.SlugExists(request.Slug);
            }
            tenant.Slug = request.Slug.ToLower();
        }

        // Check for duplicate domain if changed
        if (request.Domain != tenant.Domain && !string.IsNullOrEmpty(request.Domain))
        {
            if (await _context.Tenants.AnyAsync(t => t.Domain == request.Domain && t.Id != id))
            {
                throw ConflictException.DomainExists(request.Domain);
            }
        }

        // Update fields
        if (!string.IsNullOrEmpty(request.Name)) tenant.Name = request.Name;
        if (request.Domain != null) tenant.Domain = request.Domain;
        if (request.LogoUrl != null) tenant.LogoUrl = request.LogoUrl;
        if (request.ContactEmail != null) tenant.ContactEmail = request.ContactEmail;
        if (request.ContactPhone != null) tenant.ContactPhone = request.ContactPhone;
        if (request.PlanId.HasValue) tenant.PlanId = request.PlanId;
        if (request.Settings != null) 
            tenant.Settings = JsonSerializer.Serialize(request.Settings);

        await _context.SaveChangesAsync();

        _logger.LogInformation("Tenant updated: {TenantId}", id);

        return Success(_mapper.Map<TenantDto>(tenant));
    }

    /// <summary>
    /// Update tenant status
    /// </summary>
    [HttpPatch("{id:guid}/status")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> UpdateTenantStatus(
        Guid id, 
        [FromBody] UpdateTenantStatusRequest request)
    {
        var tenant = await _context.Tenants.FindAsync(id);
        
        if (tenant == null)
        {
            throw NotFoundException.ForTenant(id);
        }

        var oldStatus = tenant.Status.ToString();
        tenant.Status = request.Status;

        if (request.Status == TenantStatus.Deleted)
        {
            tenant.DeletedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        // Publish domain event for real-time notification
        await _mediator.Publish(new TenantStatusChangedEvent
        {
            TenantId = tenant.Id,
            TenantName = tenant.Name,
            OldStatus = oldStatus,
            NewStatus = request.Status.ToString()
        });

        _logger.LogInformation(
            "Tenant status updated: {TenantId} - {OldStatus} -> {NewStatus}", 
            id, oldStatus, request.Status);

        return Success(_mapper.Map<TenantDto>(tenant));
    }

    /// <summary>
    /// Delete a tenant (soft delete)
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> DeleteTenant(Guid id)
    {
        var tenant = await _context.Tenants.FindAsync(id);
        
        if (tenant == null)
        {
            throw NotFoundException.ForTenant(id);
        }

        tenant.Status = TenantStatus.Deleted;
        tenant.DeletedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Tenant deleted: {TenantId}", id);

        return NoContent();
    }
}
