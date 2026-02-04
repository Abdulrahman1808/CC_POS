using Microsoft.OpenApi.Models;
using Serilog;
using SuperAdminDashboard.API.Hubs;
using SuperAdminDashboard.API.Middleware;
using SuperAdminDashboard.API.Services;
using SuperAdminDashboard.Application;
using SuperAdminDashboard.Domain.Interfaces;
using SuperAdminDashboard.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// ============================================
// Configure Serilog
// ============================================
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// ============================================
// Add Services
// ============================================

// Add Application layer services (MediatR, AutoMapper, FluentValidation)
builder.Services.AddApplication();

// Add Infrastructure layer services (EF Core, JWT, etc.)
builder.Services.AddInfrastructure(builder.Configuration);

// Add API layer services
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IRealTimeNotifier, SignalRNotifier>();

// Add Controllers
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });

// Add SignalR
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
});

// Add Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Super Admin Dashboard API",
        Version = "v1",
        Description = "Production-grade API for Super Admin Dashboard",
        Contact = new OpenApiContact
        {
            Name = "Support",
            Email = "support@example.com"
        }
    });

    // Add JWT authentication to Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy
            .WithOrigins(
                builder.Configuration["Cors:Origins"]?.Split(',') ?? 
                new[] { "http://localhost:3000", "http://localhost:5173" })
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

// Add Rate Limiting
builder.Services.AddMemoryCache();

var app = builder.Build();

// ============================================
// Configure Pipeline
// ============================================

// Use Serilog request logging
app.UseSerilogRequestLogging();

// Exception handling (must be first)
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Request logging
app.UseMiddleware<RequestLoggingMiddleware>();

// Swagger (development only)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Super Admin Dashboard API v1");
        c.RoutePrefix = "api/docs";
    });
}

// HTTPS redirection
app.UseHttpsRedirection();

// CORS
app.UseCors("AllowReactApp");

// Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

// Health checks
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
    .WithTags("Health");

app.MapGet("/ready", async (SuperAdminDashboard.Infrastructure.Data.ApplicationDbContext db) =>
{
    try
    {
        await db.Database.CanConnectAsync();
        return Results.Ok(new { status = "ready", database = "connected" });
    }
    catch
    {
        return Results.StatusCode(503);
    }
}).WithTags("Health");

// Map Controllers
app.MapControllers();

// Map SignalR Hub
app.MapHub<AdminDashboardHub>("/hubs/admin-dashboard");

// ============================================
// Run Application
// ============================================
try
{
    Log.Information("Starting Super Admin Dashboard API...");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
