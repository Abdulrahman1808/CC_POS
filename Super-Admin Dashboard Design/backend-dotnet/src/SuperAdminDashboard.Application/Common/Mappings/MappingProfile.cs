using AutoMapper;
using SuperAdminDashboard.Domain.Entities;
using SuperAdminDashboard.Application.Features.Auth.DTOs;
using SuperAdminDashboard.Application.Features.Tenants.DTOs;
using SuperAdminDashboard.Application.Features.Users.DTOs;
using SuperAdminDashboard.Application.Features.Analytics.DTOs;
using SuperAdminDashboard.Application.Features.Settings.DTOs;

namespace SuperAdminDashboard.Application.Common.Mappings;

/// <summary>
/// AutoMapper profile for all entity-to-DTO mappings
/// </summary>
public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // User mappings
        CreateMap<User, UserDto>()
            .ForMember(d => d.FullName, opt => opt.MapFrom(s => s.FullName));
        CreateMap<User, CurrentUserDto>();
        
        // Tenant mappings
        CreateMap<Tenant, TenantDto>()
            .ForMember(d => d.PlanName, opt => opt.MapFrom(s => s.Plan != null ? s.Plan.Name : null));
        CreateMap<Tenant, TenantDetailDto>()
            .ForMember(d => d.CustomersCount, opt => opt.MapFrom(s => s.Customers.Count))
            .ForMember(d => d.SubscriptionsCount, opt => opt.MapFrom(s => s.Subscriptions.Count));
        CreateMap<CreateTenantRequest, Tenant>();
        CreateMap<UpdateTenantRequest, Tenant>();
        
        // Plan mappings
        CreateMap<Plan, PlanDto>();
        
        // Subscription mappings
        CreateMap<Subscription, SubscriptionDto>();
        
        // Settings mappings
        CreateMap<SystemSetting, SettingDto>();
        CreateMap<CreateSettingRequest, SystemSetting>();
        
        // Audit log mappings
        CreateMap<AuditLog, AuditLogDto>()
            .ForMember(d => d.UserEmail, opt => opt.MapFrom(s => s.User != null ? s.User.Email : null))
            .ForMember(d => d.TenantName, opt => opt.MapFrom(s => s.Tenant != null ? s.Tenant.Name : null));
    }
}
