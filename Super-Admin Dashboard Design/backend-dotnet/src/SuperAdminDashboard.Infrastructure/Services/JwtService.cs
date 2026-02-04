using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using SuperAdminDashboard.Domain.Entities;
using SuperAdminDashboard.Domain.Enums;

namespace SuperAdminDashboard.Infrastructure.Services;

public interface IJwtService
{
    string GenerateAccessToken(User user, string? sessionId = null);
    string GenerateRefreshToken();
    ClaimsPrincipal? ValidateToken(string token);
    string HashRefreshToken(string refreshToken);
}

public class JwtService : IJwtService
{
    private readonly IConfiguration _configuration;
    private readonly string _accessSecret;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _accessExpirationMinutes;
    private readonly int _refreshExpirationDays;

    public JwtService(IConfiguration configuration)
    {
        _configuration = configuration;
        _accessSecret = configuration["Jwt:AccessSecret"] ?? throw new ArgumentNullException("Jwt:AccessSecret not configured");
        _issuer = configuration["Jwt:Issuer"] ?? "SuperAdminDashboard";
        _audience = configuration["Jwt:Audience"] ?? "SuperAdminDashboard";
        _accessExpirationMinutes = int.Parse(configuration["Jwt:AccessExpirationMinutes"] ?? "15");
        _refreshExpirationDays = int.Parse(configuration["Jwt:RefreshExpirationDays"] ?? "7");
    }

    public string GenerateAccessToken(User user, string? sessionId = null)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_accessSecret));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.Role, user.Role.ToString()),
            new("role", user.Role.ToString().ToLowerInvariant())
        };

        if (!string.IsNullOrEmpty(user.FirstName))
        {
            claims.Add(new Claim("given_name", user.FirstName));
        }

        if (!string.IsNullOrEmpty(user.LastName))
        {
            claims.Add(new Claim("family_name", user.LastName));
        }

        if (!string.IsNullOrEmpty(sessionId))
        {
            claims.Add(new Claim("session_id", sessionId));
        }

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_accessExpirationMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    public ClaimsPrincipal? ValidateToken(string token)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_accessSecret)),
            ValidateIssuer = true,
            ValidIssuer = _issuer,
            ValidateAudience = true,
            ValidAudience = _audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };

        try
        {
            var principal = tokenHandler.ValidateToken(token, validationParameters, out _);
            return principal;
        }
        catch
        {
            return null;
        }
    }

    public string HashRefreshToken(string refreshToken)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(refreshToken);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }
}
