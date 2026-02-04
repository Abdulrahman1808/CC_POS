using Microsoft.Extensions.DependencyInjection;
using FluentValidation;
using MediatR;
using SuperAdminDashboard.Application.Common.Behaviors;
using System.Reflection;

namespace SuperAdminDashboard.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();
        
        // Add MediatR
        services.AddMediatR(cfg => {
            cfg.RegisterServicesFromAssembly(assembly);
        });
        
        // Add MediatR pipeline behaviors
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        
        // Add FluentValidation validators
        services.AddValidatorsFromAssembly(assembly);
        
        // Add AutoMapper
        services.AddAutoMapper(assembly);
        
        return services;
    }
}
