using System;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Authentication.Application;

/// <summary>
/// Provides CAPTCHA verification services with security enhancements
/// </summary>
public class CheckboxCaptchaService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IDistributedCache _cache;
    private readonly ILogger<CheckboxCaptchaService> _logger;
    private readonly CaptchaOptions _options;

    private const string CAPTCHA_PREFIX = "captcha:";

    public CheckboxCaptchaService(
        IHttpContextAccessor httpContextAccessor,
        IDistributedCache cache,
        ILogger<CheckboxCaptchaService> logger,
        IOptions<CaptchaOptions> options)
    {
        _httpContextAccessor = httpContextAccessor;
        _cache = cache;
        _logger = logger;
        _options = options.Value;
    }

    /// <summary>
    /// Generates a secure CAPTCHA token with automatic expiration
    /// </summary>
    public string GenerateCaptchaToken()
    {
        // Generate cryptographically secure random token
        var tokenBytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(tokenBytes);
        
        // Convert to URL-safe base64 string
        var token = Convert.ToBase64String(tokenBytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .Replace("=", "");

        // Get the IP address or session ID to prevent mass token generation
        string identifier = GetClientIdentifier();
        
        // Check for rate limiting
        string rateLimitKey = $"captcha-rate:{identifier}";
        byte[] rateLimitData = _cache.Get(rateLimitKey);
        int requestCount = 0;
        
        if (rateLimitData != null)
        {
            requestCount = BitConverter.ToInt32(rateLimitData);
            if (requestCount >= _options.MaxRequestsPerInterval)
            {
                _logger.LogWarning("CAPTCHA rate limit exceeded for {Identifier}", identifier);
                // Return empty token to indicate rate limit (handled by caller)
                return string.Empty;
            }
        }`
        
        // Store token in distributed cache with expiration
        string cacheKey = $"{CAPTCHA_PREFIX}{token}";
        _cache.Set(
            cacheKey, 
            Encoding.UTF8.GetBytes(identifier), 
            new DistributedCacheEntryOptions 
            { 
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_options.TokenExpirationMinutes) 
            });
        
        // Update rate limiting counter
        _cache.Set(
            rateLimitKey,
            BitConverter.GetBytes(requestCount + 1),
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_options.RateLimitWindowMinutes)
            });
        
        // Also store in session as a fallback if distributed cache fails
        var context = _httpContextAccessor.HttpContext;
        if (context?.Session != null)
        {
            context.Session.SetString("CaptchaToken", token);
        }

        _logger.LogDebug("Generated CAPTCHA token for {Identifier}", identifier);
        return token;
    }

    /// <summary>
    /// Validates a CAPTCHA token with secure comparison
    /// </summary>
    public bool ValidateCaptchaToken(string userToken)
    {
        if (string.IsNullOrEmpty(userToken))
        {
            _logger.LogWarning("Empty CAPTCHA token received");
            return false;
        }
        
        string identifier = GetClientIdentifier();
        string cacheKey = $"{CAPTCHA_PREFIX}{userToken}";
        
        // Try to get token from distributed cache
        byte[] storedData = _cache.Get(cacheKey);
        bool isValid = false;
        
        if (storedData != null)
        {
            string storedIdentifier = Encoding.UTF8.GetString(storedData);
            
            // Validate that this token belongs to the same client
            if (SecureEquals(storedIdentifier, identifier))
            {
                isValid = true;
                
                // Remove the token to prevent reuse
                _cache.Remove(cacheKey);
                
                _logger.LogDebug("CAPTCHA token validated successfully for {Identifier}", identifier);
            }
            else
            {
                _logger.LogWarning("CAPTCHA token identifier mismatch. Expected: {Expected}, Actual: {Actual}", 
                    storedIdentifier, identifier);
            }
        }
        else
        {
            // Fallback to session if distributed cache fails
            var context = _httpContextAccessor.HttpContext;
            if (context?.Session != null)
            {
                var storedToken = context.Session.GetString("CaptchaToken");
                if (SecureEquals(storedToken, userToken))
                {
                    isValid = true;
                    // Clear from session to prevent reuse
                    context.Session.Remove("CaptchaToken");
                    _logger.LogDebug("CAPTCHA token validated from session for {Identifier}", identifier);
                }
            }
            
            if (!isValid)
            {
                _logger.LogWarning("Invalid or expired CAPTCHA token from {Identifier}", identifier);
            }
        }
        
        return isValid;
    }
    
    /// <summary>
    /// Gets a unique identifier for the current client
    /// </summary>
    private string GetClientIdentifier()
    {
        var context = _httpContextAccessor.HttpContext;
        if (context == null)
        {
            return "unknown";
        }
        
        // Get IP address as primary identifier
        string ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        
        // Add user agent for additional entropy
        string userAgent = context.Request.Headers["User-Agent"].ToString();
        if (!string.IsNullOrEmpty(userAgent))
        {
            // Create a hash of the user agent to keep the identifier short
            using var sha = SHA256.Create();
            var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(userAgent));
            var userAgentHash = Convert.ToBase64String(hash).Substring(0, 8);
            
            return $"{ipAddress}_{userAgentHash}";
        }
        
        return ipAddress;
    }
    
    /// <summary>
    /// Performs constant-time comparison of strings to prevent timing attacks
    /// </summary>
    private bool SecureEquals(string a, string b)
    {
        if (a == null || b == null)
        {
            return false;
        }
        
        if (a.Length != b.Length)
        {
            return false;
        }
        
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(a),
            Encoding.UTF8.GetBytes(b));
    }
}

/// <summary>
/// Configuration options for the CAPTCHA service
/// </summary>
public class CaptchaOptions
{
    /// <summary>
    /// Maximum number of CAPTCHA requests allowed per client in the time window
    /// </summary>
    public int MaxRequestsPerInterval { get; set; } = 5;
    
    /// <summary>
    /// Time window for rate limiting in minutes
    /// </summary>
    public int RateLimitWindowMinutes { get; set; } = 10;
    
    /// <summary>
    /// How long a CAPTCHA token remains valid in minutes
    /// </summary>
    public int TokenExpirationMinutes { get; set; } = 15;
}