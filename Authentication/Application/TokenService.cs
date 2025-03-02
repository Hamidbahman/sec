using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Application;

public class TokenService
{
    private readonly string _secretKey;
    private readonly string _issuer;
    private readonly string _audience;
    private const int TokenExpirationMinutes = 60; // 1 hour token expiration
    private const int RefreshTokenExpirationDays = 7; // 7 days refresh token expiration

    public TokenService(IConfiguration configuration)
    {
        _secretKey = configuration["AccessToken:SecretKey"]
            ?? throw new InvalidOperationException("Secret key is not configured");
        
        _issuer = configuration["AccessToken:Issuer"] ?? "DefaultIssuer";
        _audience = configuration["AccessToken:Audience"] ?? "DefaultAudience";
    }

    /// <summary>
    /// Generates an access token with comprehensive claims
    /// </summary>
    public string GenerateAccessToken(
        long userId, 
        string clientId, 
        string scope, 
        List<string>? roles = null)
    {
        // Create claims for the token
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim("client_id", clientId),
            new Claim("scope", scope)
        };

        // Add roles if provided
        if (roles != null)
        {
            foreach (var role in roles.Where(r => !string.IsNullOrEmpty(r)))
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }
        }

        // Generate security key
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // Create token
        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(TokenExpirationMinutes),
            signingCredentials: credentials
        );

        // Write token
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Generates a refresh token
    /// </summary>
    public string GenerateRefreshToken()
    {
        // Generate cryptographically secure random bytes
        var randomNumber = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomNumber);
        }

        // Convert to base64 string, making it URL-safe
        return Convert.ToBase64String(randomNumber)
            .Replace("+", "-")
            .Replace("/", "_")
            .Replace("=", "");
    }

    /// <summary>
    /// Validates an access token
    /// </summary>
    public bool ValidateToken(string token, out List<Claim> claims)
    {
        claims = new List<Claim>();

        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey)),
                ValidateIssuer = true,
                ValidIssuer = _issuer,
                ValidateAudience = true,
                ValidAudience = _audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(5) // 5 minutes tolerance for clock skew
            };

            var claimsPrincipal = tokenHandler.ValidateToken(token, validationParameters, out _);
            claims = claimsPrincipal.Claims.ToList();

            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Extracts user ID from a validated token
    /// </summary>
    public long ExtractUserIdFromToken(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadToken(token) as JwtSecurityToken;

            var userIdClaim = jwtToken?.Claims.FirstOrDefault(c => c.Type == "sub");
            
            if (userIdClaim != null && long.TryParse(userIdClaim.Value, out long userId))
            {
                return userId;
            }

            throw new ArgumentException("Unable to extract user ID from token");
        }
        catch
        {
            throw new ArgumentException("Invalid token format");
        }
    }

    /// <summary>
    /// Validates a refresh token (basic validation)
    /// </summary>
    public bool ValidateRefreshToken(string refreshToken)
    {
        // Basic validation checks
        if (string.IsNullOrEmpty(refreshToken))
            return false;

        // Check token length and format
        if (refreshToken.Length < 32)
            return false;

        // In a real-world scenario, you might want to:
        // 1. Check if the token exists in the database
        // 2. Check if the token has been used or revoked
        // 3. Check token expiration

        return true;
    }

    /// <summary>
    /// Generates token validation parameters for manual validation
    /// </summary>
    public TokenValidationParameters GetTokenValidationParameters()
    {
        return new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey)),
            ValidateIssuer = true,
            ValidIssuer = _issuer,
            ValidateAudience = true,
            ValidAudience = _audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(5)
        };
    }
}