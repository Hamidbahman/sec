using System;
using System.Text;
using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;

namespace Application
{
    public class TokenService
    {
        private readonly byte[] _secretKey;
        private const int ACCESS_TOKEN_EXPIRATION_MINUTES = 30;
        private const int REFRESH_TOKEN_EXPIRATION_DAYS = 7;

        public class TokenValidationResult
        {
            public bool IsValid { get; set; }
            public long? UserId { get; set; }
            public DateTime? IssuedAt { get; set; }
            public DateTime? ExpiresAt { get; set; }
        }

        public TokenService(IConfiguration configuration)
        {
            var secretKeyString = configuration["AccessToken:SecretKey"]
                ?? throw new InvalidOperationException("Secret key is missing");

            _secretKey = Encoding.UTF8.GetBytes(secretKeyString);
        }

        public string GenerateAccessToken(long userId)
        {
            var claims = new[]
            {
                new System.Security.Claims.Claim("userId", userId.ToString()),
                new System.Security.Claims.Claim("jti", Guid.NewGuid().ToString())
            };

            var key = new SymmetricSecurityKey(_secretKey);
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expirationTime = DateTime.UtcNow.AddMinutes(ACCESS_TOKEN_EXPIRATION_MINUTES);

            var token = new JwtSecurityToken(
                issuer: "yourIssuer", 
                audience: "yourAudience", 
                claims: claims,
                expires: expirationTime,
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public string GenerateRefreshToken()
        {
            byte[] randomBytes = new byte[64]; 
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);

            return Convert.ToBase64String(randomBytes);
        }

        public TokenValidationResult ValidateAccessToken(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = new SymmetricSecurityKey(_secretKey);
                var validationParameters = new TokenValidationParameters
                {
                    IssuerSigningKey = key,
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidIssuer = "yourIssuer",
                    ValidAudience = "yourAudience",
                    ClockSkew = TimeSpan.Zero 
                };

                SecurityToken validatedToken;
                var principal = tokenHandler.ValidateToken(token, validationParameters, out validatedToken);
                
                var jwtToken = (JwtSecurityToken)validatedToken;
                var userId = long.Parse(principal.FindFirst("userId")?.Value);

                return new TokenValidationResult
                {
                    IsValid = true,
                    UserId = userId,
                    IssuedAt = jwtToken.ValidFrom,
                    ExpiresAt = jwtToken.ValidTo
                };
            }
            catch
            {
                return new TokenValidationResult { IsValid = false };
            }
        }
    }
}
