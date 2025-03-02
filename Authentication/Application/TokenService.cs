using System;

namespace Application;

using System;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;

public class TokenService
{
    private readonly string _secretKey;
    public TokenService(IConfiguration configuration)
    {
        _secretKey = configuration["AccessToken:SecretKey"]
            ?? throw new InvalidOperationException("Secret key is not here :/");
    }

    public string GenerateAccessToken(long userId)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_secretKey));
        
        string tokenData = $"{userId}:{Guid.NewGuid()}:{DateTime.UtcNow.Ticks}";
        byte[] tokenBytes = Encoding.UTF8.GetBytes(tokenData);
        byte[] hash = hmac.ComputeHash(tokenBytes);
        
        string signature = Convert.ToBase64String(hash);
        return $"{tokenData}:{signature}";
    }

    public string GenerateRefreshToken()
    {
        // Generate a cryptographically secure random number
        var randomNumber = new byte[32]; // 32 bytes = 256 bits
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomNumber);
        }

        // Convert the random number to a base64 string
        return Convert.ToBase64String(randomNumber);
    }
}
