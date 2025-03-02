using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Authentication;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Application;
using Authentication.Domain.Entities;
using Authentication.Domain.Enums;
using Authentication.Domain.Repositories;
using Authentication.Infrastructure.Services;
using Domain.Repositories;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace Authentication.Application;
    /// <summary>
    /// Implements OAuth 2.1 authorization server functionality with OpenID Connect support
    /// </summary>
    public class OAuthService 
    {
        private readonly IApplicationRepository _applicationRepository;
        private readonly IUserPropertyRepository _userPropertyRepo;
        private readonly DistributedCacheService _cache;
        private readonly IUserRepository _userRepo;
        private readonly TokenService _tokenService;
        private readonly OtpService _otpService;
        private readonly CheckboxCaptchaService _checkBox;
        private readonly PuzzleCaptchaService _puzzleService;
        private readonly IOAuthTokenRepository _oauthRepo;
        private readonly ILogger<OAuthService> _logger;
        //private readonly IAuditLogService _auditLogService;
        private readonly IConfiguration _configuration;
        
        // Cache keys
        private const string AUTH_CODE_PREFIX = "auth_code:";
        private const string OTP_REQUEST_PREFIX = "otp_request:";
        private const string PKCE_VERIFIER_PREFIX = "pkce_verifier:";
        
        public OAuthService(
            DistributedCacheService cache,
            IOAuthTokenRepository oauthRepo,
            PuzzleCaptchaService puzzleCaptchaService,
            IUserPropertyRepository userPropertyRepository,
            TokenService tokenService,
            CheckboxCaptchaService checkboxCaptchaService,
            OtpService otpService,
            IApplicationRepository applicationRepository,
            IUserRepository userRepository,
            ILogger<OAuthService> logger,
            //IAuditLogService auditLogService,
            IConfiguration configuration)
        {
            _tokenService = tokenService;
            _oauthRepo = oauthRepo;
            _puzzleService = puzzleCaptchaService;
            _userPropertyRepo = userPropertyRepository;
            _checkBox = checkboxCaptchaService;
            _applicationRepository = applicationRepository;
            _userRepo = userRepository;
            _otpService = otpService;
            _logger = logger;
            //_auditLogService = auditLogService;
            _cache = cache;
            _configuration = configuration;
        }

        /// <summary>
        /// Validates a client application and generates an authorization code
        /// </summary>
        /// <param name="clientId">Client application ID</param>
        /// <param name="responseType">OAuth response type (code, token)</param>
        /// <param name="redirectUri">Client redirect URI</param>
        /// <param name="state">OAuth state parameter</param>
        /// <param name="scope">Requested scopes</param>
        /// <param name="nonce">Anti-replay nonce</param>
        /// <param name="codeChallenge">PKCE code challenge</param>
        /// <param name="codeChallengeMethod">PKCE challenge method (S256)</param>
        /// <param name="userCaptchaToken">CAPTCHA validation token if required</param>
        /// <returns>Authorization code or error</returns>
        public async Task<AuthorizationResult> AuthorizeAsync(
    string clientId, 
    string responseType, 
    string redirectUri, 
    string? state = null,
    string? scope = null,
    string? userCaptchaToken = null)
{
    try
    {
        _logger.LogInformation("Authorization request for client ID: {ClientId}", clientId);
        
        // Validate response type
        if (responseType != "code")
        {
            return new AuthorizationResult
            {
                IsSuccess = false,
                Error = "unsupported_response_type",
                ErrorDescription = "Only 'code' response type is supported"
            };
        }
        
        // Validate client
        var application = await _applicationRepository.GetApplicationByClientIdAsync(clientId);
        if (application == null)
        {
            _logger.LogWarning("Client ID not found: {ClientId}", clientId);
            
            return new AuthorizationResult
            {
                IsSuccess = false,
                Error = "unauthorized_client",
                ErrorDescription = "Client not found"
            };
        }
        
        // Validate redirect URI
        if (!IsRedirectUriValid(application.RedirectUrls, redirectUri))
        {
            _logger.LogWarning("Invalid redirect URI: {RedirectUri} for client: {ClientId}", redirectUri, clientId);
            
            return new AuthorizationResult
            {
                IsSuccess = false,
                Error = "invalid_request",
                ErrorDescription = "Redirect URI is not allowed"
            };
        }
        
        // Get client's lock configuration
        var configLock = await _applicationRepository.GetConfigurationLockAsync(clientId);
        
        // Track failed attempts by client ID
        string failedAttemptsKey = $"failed_attempts:{clientId}";
        int failedAttempts = await _cache.GetAsync<int>(failedAttemptsKey) ?? 0;
        failedAttempts++;
        
        await _cache.SetAsync(failedAttemptsKey, failedAttempts, TimeSpan.FromHours(1));
        
        // Enable CAPTCHA after 3 failed attempts
        if (failedAttempts > 3)
        {
            configLock.EnableCaptcha();
            await _applicationRepository.SaveChangesAsync();
        }

        // CAPTCHA validation
        if (configLock.CaptchaNeeded)
        {
            // No CAPTCHA token provided
            if (string.IsNullOrEmpty(userCaptchaToken))
            {
                _logger.LogInformation("CAPTCHA required for client: {ClientId}", clientId);
                
                return new AuthorizationResult
                {
                    IsSuccess = false,
                    RequiresCaptcha = true,
                    Error = "captcha_required",
                    ErrorDescription = "CAPTCHA verification required"
                };
            }

            // Validate provided CAPTCHA token
            if (!_checkBox.ValidateCaptchaToken(userCaptchaToken))
            {
                _logger.LogWarning("Invalid CAPTCHA token for client: {ClientId}", clientId);
                
                return new AuthorizationResult
                {
                    IsSuccess = false,
                    Error = "invalid_captcha",
                    ErrorDescription = "Invalid CAPTCHA"
                };
            }
        }



        // Generate authorization code
        string authCode = GenerateSecureAuthCode();
        
        // Store authorization code information
        var authCodeInfo = new AuthCodeInfo
        {
            ClientId = clientId,
            RedirectUri = redirectUri,
            Scope = scope ?? application.ClientScope,
            State = state,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(10) 
        };
        
        // Store in distributed cache
        await _cache.SetAsync($"{AUTH_CODE_PREFIX}{authCode}", authCodeInfo, TimeSpan.FromMinutes(10));
        
        // Reset failed attempts on successful authorization
        await _cache.SetAsync(failedAttemptsKey, 0, TimeSpan.FromHours(1));
        
        _logger.LogInformation("Authorization code generated successfully for client: {ClientId}", clientId);
        
        return new AuthorizationResult
        {
            IsSuccess = true,
            AuthorizationCode = authCode,
            State = state
        };
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error generating authorization code for client: {ClientId}", clientId);
        
        return new AuthorizationResult
        {
            IsSuccess = false,
            Error = "server_error",
            ErrorDescription = "An unexpected error occurred"
        };
    }
}

        /// <summary>
        /// Authenticates a user with username, password, and auth code
        /// </summary>
        public async Task<AuthResult> LoginAsync(string username, string password, string authenticationCode)
        {
            try
            {
                _logger.LogInformation("Login attempt for user: {Username}", username);
                var ipAddress = GetCurrentIpAddress();
                
                // Validate authentication code
                var token = await _oauthRepo.GetByAccessTokenAsync(authenticationCode);
                if (token == null)
                {
                    _logger.LogWarning("Invalid authentication token during login for user: {Username}", username);
                    //await _auditLogService.LogSecurityEventAsync("AUTH_FAILURE", $"Invalid auth token, user: {username}");
                    
                    return new AuthResult
                    {
                        Success = false,
                        Message = "Invalid authentication token",
                        Error = "invalid_token",
                        TwoFactorRequired = false
                    };
                }

                var user = await _userRepo.GetByUsernameAsync(username);
                
                // Don't reveal if user exists
                if (user == null)
                {
                    _logger.LogWarning("Login attempt for non-existent user: {Username}", username);
                    //await _auditLogService.LogSecurityEventAsync("AUTH_FAILURE", $"Non-existent user: {username}");
                    
                    return new AuthResult
                    {
                        Success = false,
                        Message = "Invalid username or password",
                        Error = "invalid_credentials",
                        TwoFactorRequired = false
                    };
                }

                // Check if account is already locked
                var logPol = await _userRepo.GetLoginPoliciesByUserID(user.Id.ToString());
                if (logPol != null && logPol.LockTypes == LockTypes.TemporaryLock)
                {
                    // Check if lock has expired
                    if (logPol.LockEndDateTime > DateTime.UtcNow)
                    {
                        _logger.LogWarning("Login attempt for locked account: {Username}", username);
                        //await _auditLogService.LogSecurityEventAsync("LOCKED_ACCOUNT_ACCESS_ATTEMPT", $"User: {username}");
                        
                        // Calculate remaining lock time
                        var remainingLockTime = (int)(logPol.LockEndDateTime - DateTime.UtcNow).TotalMinutes;
                        
                        return new AuthResult
                        {
                            Success = false,
                            Message = $"Account is temporarily locked. Please try again in approximately {remainingLockTime} minute(s).",
                            Error = "account_locked",
                            TwoFactorRequired = false,
                            LockDuration = remainingLockTime
                        };
                    }
                    else
                    {
                        // Lock has expired, reset it
                        logPol.SetLockType(LockTypes.None);
                        user.ResetLoginAttempt();
                        await _userRepo.SaveChangesAsync();
                    }
                }

                // Validate password - THIS SHOULD BE HASHED IN PRODUCTION
                // In a real implementation, use BCrypt or Argon2id for password verification
                bool passwordValid = user.UserProperty.Password == password;
                
                if (!passwordValid)
                {
                    user.IncrementLoginAttempt();
                    await _userRepo.SaveChangesAsync();
                    
                    _logger.LogWarning("Failed login attempt ({Attempts}) for user: {Username}", 
                        user.LoginAttempt, username);
                    
                    //await _auditLogService.LogSecurityEventAsync("FAILED_LOGIN", 
                    //    $"User: {username}, Attempts: {user.LoginAttempt}, IP: {ipAddress}");
                    
                    // Check if we need to lock the account
                    if (logPol != null && user.LoginAttempt > 5)
                    {
                        logPol.SetLockType(LockTypes.TemporaryLock);
                        // Set lock duration (should come from config)
                        var lockDuration = TimeSpan.FromMinutes(30);
                        
                        // Add these methods to the LoginPolicy class
                        SetLoginPolicyLockDuration(logPol, lockDuration);
                        
                        await _userRepo.SaveChangesAsync();
                        
                        _logger.LogWarning("Account locked due to too many failed attempts: {Username}", username);
                        //await _auditLogService.LogSecurityEventAsync("ACCOUNT_LOCKED", 
                        //    $"User: {username}, Duration: {lockDuration}");
                        
                        return new AuthResult
                        {
                            Success = false,
                            Message = "Account has been temporarily locked due to too many failed attempts.",
                            Error = "account_locked",
                            TwoFactorRequired = false,
                            LockDuration = (int)lockDuration.TotalMinutes
                        };
                    }
                    
                    // How many attempts left before locking
                    var attemptsLeft = 5 - user.LoginAttempt;
                    var warningMessage = attemptsLeft > 0 
                        ? $"Invalid username or password. {attemptsLeft} attempt(s) remaining before your account is locked." 
                        : "Invalid username or password.";
                    
                    return new AuthResult
                    {
                        Success = false,
                        Message = warningMessage,
                        Error = "invalid_credentials",
                        TwoFactorRequired = false,
                        AttemptsRemaining = attemptsLeft
                    };
                }

                // Password is valid, reset login attempts
                user.ResetLoginAttempt();
                await _userRepo.SaveChangesAsync();
                
                _logger.LogInformation("Successful login for user: {Username}", username);
                //await _auditLogService.LogSecurityEventAsync("SUCCESSFUL_LOGIN", 
                //    $"User: {username}, IP: {ipAddress}");

                // Handle 2FA if enabled
                if (!user.TwoFactorEnabled)
                {
                    // Update token with user info
                    token.SetUserName(user.Username);
                    await _oauthRepo.UpdateAsync(token);
                    
                    // Get user roles for additional claims
                    var userWithRole = await _userRepo.GetByUsernameAsync(username);
                    var roles = userWithRole.UserRoles;

                    // Generate new token with user details
                    var application = await _applicationRepository.GetApplicationByClientIdAsync(token.ClientId);
                    var scopes = application?.ClientScope?.Split(' ') ?? new[] { "profile" };
                    
                    // Regenerate access token with user ID and roles

                    var newAccessToken = _tokenService.GenerateAccessToken(
                        user.Id,
                        token.ClientId,
                        string.Join(" ", scopes)
                    );
                    
                    token.AccessToken = newAccessToken;
                    await _oauthRepo.UpdateAsync(token);

                    return new AuthResult
                    {
                        Success = true,
                        Token = newAccessToken,
                        RefreshToken = token.RefreshToken,
                        TwoFactorRequired = false,
                        ExpiresIn = 3600, // 1 hour token expiration
                        TokenType = "Bearer",
                        Scope = string.Join(" ", scopes)
                    };
                }

                // 2FA is enabled, generate and send OTP
                var otpCode =  _otpService.GenerateOtp(user.Id);
                 _otpService.SendOtp(user.PhoneNumber, otpCode);
                
                // Store user ID in token for OTP verification
                token.SetUserName(user.Username);
                await _oauthRepo.UpdateAsync(token);
                
                _logger.LogInformation("OTP sent for user: {Username}", username);
                //await _auditLogService.LogSecurityEventAsync("OTP_SENT", $"User: {username}");

                return new AuthResult
                {
                    Success = false,
                    Message = "OTP Required",
                    Error = "otp_required",
                    TwoFactorRequired = true,
                    PendingToken = token.AccessToken,
                    PhoneHint = MaskPhoneNumber(user.PhoneNumber)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for user: {Username}", username);
                throw;
            }
        }

        /// <summary>
        /// Verifies OTP code for two-factor authentication
        /// </summary>
        public async Task<AuthResult> VerifyOtpAsync(string username, string otpCode, string pendingToken)
        {
            try
            {
                _logger.LogInformation("OTP verification attempt for user: {Username}", username);
                
                // Verify pending token first
                var token = await _oauthRepo.GetByAccessTokenAsync(pendingToken);
                if (token == null || token.UserName != username)
                {
                    _logger.LogWarning("Invalid pending token during OTP verification for user: {Username}", username);
                    //await _auditLogService.LogSecurityEventAsync("OTP_VERIFICATION_FAILURE", $"Invalid token, user: {username}");
                    
                    return new AuthResult
                    {
                        Success = false,
                        Message = "Invalid authentication session",
                        Error = "invalid_grant",
                        TwoFactorRequired = false
                    };
                }
                
                var user = await _userRepo.GetByUsernameAsync(username);
                
                if (user == null)
                {
                    _logger.LogWarning("OTP verification attempt for non-existent user: {Username}", username);
                    //await _auditLogService.LogSecurityEventAsync("OTP_VERIFICATION_FAILURE", $"Non-existent user: {username}");
                    
                    return new AuthResult
                    {
                        Success = false,
                        Message = "Invalid username or OTP",  // Generic error message
                        Error = "invalid_grant",
                        TwoFactorRequired = false
                    };
                }

                var confPass = await _userPropertyRepo.GetConfigurationPasswordByUserIdAsync(user.UserProperty.ConfigurationPasswordId);

                if (confPass == null)
                {
                    _logger.LogWarning("Missing password configuration for user: {Username}", username);
                    
                    return new AuthResult
                    {
                        Success = false,
                        Message = "Authentication configuration error",
                        Error = "server_error",
                        TwoFactorRequired = false
                    };
                }
                
                // Check if password is expired
                var expirationD = confPass.CreateDate.AddDays(confPass.ExpireDaysAmount);
                if (expirationD <= DateTime.UtcNow)
                {
                    _logger.LogInformation("Password expired for user: {Username}", username);
                   // await _auditLogService.LogSecurityEventAsync("PASSWORD_EXPIRED", $"User: {username}");
                    
                    return new AuthResult
                    {
                        Success = false,
                        Message = "Password expired. Please change your password.",
                        Error = "password_expired",
                        TwoFactorRequired = false,
                        PasswordChangeRequired = true
                    };
                }

                // Validate IP restrictions if enabled
                if (!string.IsNullOrEmpty(user.IpRange) && !IsIpAllowed(GetCurrentIpAddress(), user.IpRange))
                {
                    _logger.LogWarning("IP restriction blocked access for user: {Username}", username);
                   // await _auditLogService.LogSecurityEventAsync("IP_RESTRICTION", $"User: {username}, IP: {GetCurrentIpAddress()}");
                    
                    return new AuthResult
                    {
                        Success = false,
                        Message = "Access denied from this location",
                        Error = "access_denied",
                        TwoFactorRequired = false
                    };
                }

                // Validate OTP code
                bool isOtpValid =  _otpService.ValidateOtp(user.Id, otpCode);
                if (!isOtpValid)
                {
                    _logger.LogWarning("Invalid OTP for user: {Username}", username);
                   // await _auditLogService.LogSecurityEventAsync("OTP_VERIFICATION_FAILURE", $"User: {username}");
                    
                    return new AuthResult
                    {
                        Success = false,
                        Message = "Invalid OTP code",
                        Error = "invalid_otp",
                        TwoFactorRequired = true,
                        PendingToken = pendingToken
                    };
                }

                // OTP is valid, generate token with user details
                var application = await _applicationRepository.GetApplicationByClientIdAsync(token.ClientId);
                var scopes = application?.ClientScope?.Split(' ') ?? new[] { "profile" };
                
                // Get user roles for additional claims
                var roles = await _userRepo.GetUserRolesAsync(user.Id);
                
                // Generate token with user details
                var accessToken = _tokenService.GenerateAccessToken(
                    user.Id,
                    token.ClientId,
                    string.Join(" ", scopes)
                );
                
                var refreshToken = _tokenService.GenerateRefreshToken();
                
                // Update token in database
                token.AccessToken = accessToken;
                token.RefreshToken = refreshToken;
                await _oauthRepo.UpdateAsync(token);
                
                _logger.LogInformation("Successful OTP verification for user: {Username}", username);
                //await _auditLogService.LogSecurityEventAsync("SUCCESSFUL_OTP_VERIFICATION", $"User: {username}");

                return new AuthResult
                {
                    Success = true,
                    Token = accessToken,
                    RefreshToken = refreshToken,
                    TwoFactorRequired = false,
                    Message = "Authentication successful",
                    ExpiresIn = 3600, // 1 hour
                    TokenType = "Bearer",
                    Scope = string.Join(" ", scopes)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during OTP verification for user: {Username}", username);
                throw;
            }
        }

        /// <summary>
        /// Refresh an access token using a refresh token
        /// </summary>
        public async Task<TokenResult> RefreshTokenAsync(string refreshToken, string clientId, string clientSecret)
        {
            try
            {
                _logger.LogInformation("Token refresh requested for client: {ClientId}", clientId);
                
                // Validate client credentials
                var application = await _applicationRepository.GetApplicationByClientIdAsync(clientId);
                if (application == null || !SecureCompare(clientSecret, application.ClientSecret))
                {
                    _logger.LogWarning("Invalid client credentials during token refresh: {ClientId}", clientId);
                    //await _auditLogService.LogSecurityEventAsync("AUTH_FAILURE", $"Invalid client credentials, client: {clientId}");
                    
                    return new TokenResult
                    {
                        IsSuccess = false,
                        Error = "invalid_client",
                        ErrorDescription = "Invalid client credentials"
                    };
                }
                
                // Validate refresh token
                var tokenEntity = await _oauthRepo.GetByRefreshTokenAsync(refreshToken);
                if (tokenEntity == null || tokenEntity.ClientId != clientId)
                {
                    _logger.LogWarning("Invalid refresh token for client: {ClientId}", clientId);
                    //await _auditLogService.LogSecurityEventAsync("INVALID_REFRESH_TOKEN", $"Client: {clientId}");
                    
                    return new TokenResult
                    {
                        IsSuccess = false,
                        Error = "invalid_grant",
                        ErrorDescription = "Invalid refresh token"
                    };
                }
                
                // Get user by username
                var user = await _userRepo.GetByUsernameAsync(tokenEntity.UserName);
                if (user == null)
                {
                    _logger.LogWarning("User not found for refresh token: {Username}", tokenEntity.UserName);
                    
                    return new TokenResult
                    {
                        IsSuccess = false,
                Error = "invalid_grant",
                        ErrorDescription = "User not found"
                    };
                }
                
                // Check if user is locked or disabled
                var logPol = await _userRepo.GetLoginPoliciesByUserID(user.Id.ToString());
                if (logPol != null && logPol.LockTypes == LockTypes.TemporaryLock && logPol.LockEndDateTime > DateTime.UtcNow)
                {
                    _logger.LogWarning("Refresh token attempt for locked account: {Username}", user.Username);
                    //await _auditLogService.LogSecurityEventAsync("LOCKED_ACCOUNT_TOKEN_REFRESH", $"User: {user.Username}");
                    
                    return new TokenResult
                    {
                        IsSuccess = false,
                        Error = "invalid_grant",
                        ErrorDescription = "Account is locked"
                    };
                }
                
                // Generate new tokens
                var applicationC = await _applicationRepository.GetApplicationByClientIdAsync(tokenEntity.ClientId);
                var scopes = application?.ClientScope?.Split(' ') ?? new[] { "profile" };
                
                // Get user roles for additional claims
                var roles = await _userRepo.GetUserRolesAsync(user.Id);
                
                var newAccessToken = _tokenService.GenerateAccessToken(
                    user.Id,
                    tokenEntity.ClientId,
                    string.Join(" ", scopes)
                );
                
                var newRefreshToken = _tokenService.GenerateRefreshToken();
                
                // Update tokens in database
                tokenEntity.AccessToken = newAccessToken;
                tokenEntity.RefreshToken = newRefreshToken;
                await _oauthRepo.UpdateAsync(tokenEntity);
                
                _logger.LogInformation("Token refreshed for user: {Username}", user.Username);
                //await _auditLogService.LogSecurityEventAsync("TOKEN_REFRESHED", $"User: {user.Username}");

                return new TokenResult
                {
                    IsSuccess = true,
                    AccessToken = newAccessToken,
                    RefreshToken = newRefreshToken,
                    ExpiresIn = 3600,
                    TokenType = "Bearer",
                    Scope = string.Join(" ", scopes)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing token");
                
                return new TokenResult
                {
                    IsSuccess = false,
                    Error = "server_error",
                    ErrorDescription = "An unexpected error occurred"
                };
            }
        }

        /// <summary>
        /// Revokes an access or refresh token
        /// </summary>
        public async Task<bool> RevokeTokenAsync(string token, string clientId, string clientSecret, string tokenTypeHint)
        {
            try
            {
                _logger.LogInformation("Token revocation requested for client: {ClientId}", clientId);
                
                // Validate client credentials
                var application = await _applicationRepository.GetApplicationByClientIdAsync(clientId);
                if (application == null || !SecureCompare(clientSecret, application.ClientSecret))
                {
                    _logger.LogWarning("Invalid client credentials during token revocation: {ClientId}", clientId);
                    //await _auditLogService.LogSecurityEventAsync("AUTH_FAILURE", $"Invalid client credentials, client: {clientId}");
                    return false;
                }
                
                // Find token based on type hint
                OauthToken tokenEntity = null;
                
                if (tokenTypeHint == "refresh_token")
                {
                    tokenEntity = await _oauthRepo.GetByRefreshTokenAsync(token);
                }
                else if (tokenTypeHint == "access_token")
                {
                    tokenEntity = await _oauthRepo.GetByAccessTokenAsync(token);
                }
                else
                {
                    // Try both if hint not provided
                    tokenEntity = await _oauthRepo.GetByAccessTokenAsync(token) 
                              ?? await _oauthRepo.GetByRefreshTokenAsync(token);
                }
                
                if (tokenEntity == null || tokenEntity.ClientId != clientId)
                {
                    _logger.LogWarning("Invalid token for revocation, client: {ClientId}", clientId);
                    return false;
                }
                
                // Invalidate token
                await _oauthRepo.DeleteAsync(tokenEntity.Id);
                
                _logger.LogInformation("Token revoked for user: {Username}, client: {ClientId}", 
                    tokenEntity.UserName, clientId);
                    
                //await _auditLogService.LogSecurityEventAsync("TOKEN_REVOKED", 
                //    $"User: {tokenEntity.UserName}, Client: {clientId}");
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking token");
                return false;
            }
        }

        /// <summary>
        /// Changes a user's password with policy validation
        /// </summary>
        public async Task<PassResult> ChangePassword(string username, string exPassword, string newPassword, string confirmPassword)
        {
            try
            {
                _logger.LogInformation("Password change requested for user: {Username}", username);
                
                var user = await _userRepo.GetByUsernameAsync(username);
                if (user == null)
                {
                    _logger.LogWarning("Password change attempted for non-existent user: {Username}", username);
                    //await _auditLogService.LogSecurityEventAsync("PASSWORD_CHANGE_FAILURE", $"Non-existent user: {username}");
                    
                    return new PassResult
                    {
                        Success = false,
                        Password = null,
                        Message = "User not found",
                        Error = "invalid_user"
                    };
                }

                // Check if old password matches - SHOULD USE HASHED COMPARISON IN PRODUCTION
                if (exPassword != user.UserProperty.Password)
                {
                    _logger.LogWarning("Incorrect current password during change attempt: {Username}", username);
                    //await _auditLogService.LogSecurityEventAsync("PASSWORD_CHANGE_FAILURE", $"Incorrect current password: {username}");
                    
                    return new PassResult
                    {
                        Success = false,
                        Password = null,
                        Message = "Current password is incorrect",
                        Error = "invalid_password"
                    };
                }

                // Get password configuration
                var confPass = await _userPropertyRepo.GetConfigurationPasswordByUserIdAsync(user.UserProperty.ConfigurationPasswordId);
                if (confPass == null)
                {
                    _logger.LogError("Missing password configuration for user: {Username}", username);
                    
                    return new PassResult
                    {
                        Success = false,
                        Password = null,
                        Message = "Password configuration not found",
                        Error = "configuration_error"
                    };
                }

                // Validate new password
                var validationResult = ValidatePassword(newPassword, confirmPassword, confPass);
                if (!validationResult.IsValid)
                {
                    _logger.LogWarning("Password validation failed: {Reason}, User: {Username}", 
                        validationResult.ErrorMessage, username);
                    
                    return new PassResult
                    {
                        Success = false,
                        Password = null,
                        Message = validationResult.ErrorMessage,
                        Error = "password_policy_violation"
                    };
                }
                
                // Check password history to prevent reuse if policy requires it
                // if (confPass.IsPolicyNeeded)
                // {
                //     try
                //     {
                //         // This requires implementing password history in the UserProperty entity
                //         bool isPasswordReused = await _userPropertyRepo.IsPasswordInHistoryAsync(user.Id, newPassword);
                //         if (isPasswordReused)
                //         {
                //             return new PassResult
                //             {
                //                 Success = false,
                //                 Password = null,
                //                 Message = "You cannot reuse a previous password",
                //                 Error = "password_reuse"
                //             };
                //         }
                //     }
                //     catch (NotImplementedException)
                //     {
                //         _logger.LogWarning("Password history check not implemented");
                //     }
                // }

                // Update password
                user.UserProperty.SetPassowrd(newPassword);
                await _userPropertyRepo.SaveChangesAsync();
                await _userRepo.SaveChangesAsync();
                
                _logger.LogInformation("Password changed successfully for user: {Username}", username);
                //await _auditLogService.LogSecurityEventAsync("PASSWORD_CHANGED", $"User: {username}");

                // Revoke all existing tokens to enforce re-login with new password
                await _oauthRepo.RevokeAllTokensForUserAsync(username);

                return new PassResult
                {
                    Success = true,
                    Password = null, // Don't return the password
                    Message = "Password changed successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password for user: {Username}", username);
                
                return new PassResult
                {
                    Success = false,
                    Password = null,
                    Message = "An unexpected error occurred",
                    Error = "server_error"
                };
            }
        }

        /// <summary>
        /// Initiates password reset by sending a reset code
        /// </summary>
        public async Task<ResetResult> InitiatePasswordResetAsync(string username)
        {
            try
            {
                _logger.LogInformation("Password reset initiated for user: {Username}", username);
                
                var user = await _userRepo.GetByUsernameAsync(username);
                if (user == null)
                {
                    // Don't reveal if user exists
                    return new ResetResult
                    {
                        Success = true,
                        Message = "If your account exists, a password reset link has been sent to your email."
                    };
                }
                
                // Generate reset token
                string resetToken = GenerateSecureAuthCode();
                DateTime expiryTime = DateTime.UtcNow.AddHours(1);
                
                // Store reset token
                await _cache.SetAsync($"reset_token:{resetToken}", 
                    new PasswordResetInfo
                    {
                        Username = username,
                        ExpiresAt = expiryTime
                    }, 
                    TimeSpan.FromHours(1));
                
                // Mock email sending - in production, use an email service
                string resetLink = $"https://your-app.com/reset-password?token={resetToken}";
                _logger.LogInformation("Password reset link for {Username}: {ResetLink}", username, resetLink);
                
                // In production, send via email service:
                // await _emailService.SendEmailAsync(user.Email, "Password Reset", $"Click here to reset your password: {resetLink}");
                
                //await _auditLogService.LogSecurityEventAsync("PASSWORD_RESET_INITIATED", $"User: {username}");
                
                return new ResetResult
                {
                    Success = true,
                    Message = "If your account exists, a password reset link has been sent to your email."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initiating password reset for: {Username}", username);
                
                return new ResetResult
                {
                    Success = false,
                    Message = "An unexpected error occurred",
                    Error = "server_error"
                };
            }
        }
        
        /// <summary>
        /// Completes password reset with a valid token
        /// </summary>
        public async Task<ResetResult> CompletePasswordResetAsync(string resetToken, string newPassword, string confirmPassword)
        {
            try
            {
                _logger.LogInformation("Password reset completion attempted with token");
                
                // Verify token
                var resetInfo = await _cache.GetAsync<PasswordResetInfo>($"reset_token:{resetToken}");
                if (resetInfo == null || resetInfo.ExpiresAt < DateTime.UtcNow)
                {
                    _logger.LogWarning("Invalid or expired password reset token");
                    
                    return new ResetResult
                    {
                        Success = false,
                        Message = "Invalid or expired reset token",
                        Error = "invalid_token"
                    };
                }
                
                // Get user
                var user = await _userRepo.GetByUsernameAsync(resetInfo.Username);
                if (user == null)
                {
                    _logger.LogWarning("User not found for reset token: {Username}", resetInfo.Username);
                    
                    return new ResetResult
                    {
                        Success = false,
                        Message = "User not found",
                        Error = "invalid_user"
                    };
                }
                
                // Get password configuration
                var confPass = await _userPropertyRepo.GetConfigurationPasswordByUserIdAsync(user.UserProperty.ConfigurationPasswordId);
                if (confPass == null)
                {
                    _logger.LogError("Missing password configuration for user: {Username}", resetInfo.Username);
                    
                    return new ResetResult
                    {
                        Success = false,
                        Message = "Password configuration not found",
                        Error = "configuration_error"
                    };
                }
                
                // Validate password
                var validationResult = ValidatePassword(newPassword, confirmPassword, confPass);
                if (!validationResult.IsValid)
                {
                    return new ResetResult
                    {
                        Success = false,
                        Message = validationResult.ErrorMessage,
                        Error = "password_policy_violation"
                    };
                }
                
                // Update password
                user.UserProperty.SetPassowrd(newPassword);
                await _userPropertyRepo.SaveChangesAsync();
                
                // Remove reset token
                await _cache.RemoveAsync($"reset_token:{resetToken}");
                
                // Revoke all existing tokens
                //await _oauthRepo.RevokeAllTokensForUserAsync(resetInfo.Username);
                
                _logger.LogInformation("Password reset successful for user: {Username}", resetInfo.Username);
                //await _auditLogService.LogSecurityEventAsync("PASSWORD_RESET_COMPLETE", $"User: {resetInfo.Username}");
                
                return new ResetResult
                {
                    Success = true,
                    Message = "Your password has been reset successfully."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing password reset");
                
                return new ResetResult
                {
                    Success = false,
                    Message = "An unexpected error occurred",
                    Error = "server_error"
                };
            }
        }

        /// <summary>
        /// Fetches user information (for OpenID Connect userinfo endpoint)
        /// </summary>
        public async Task<UserInfoResult> GetUserInfoAsync(string accessToken)
        {
            try
            {
                _logger.LogInformation("User info requested");
                
                // Validate access token
                if (!_tokenService.ValidateToken(accessToken, out var claims))
                {
                    _logger.LogWarning("Invalid access token during userinfo request");
                    return new UserInfoResult
                    {
                        IsSuccess = false,
                        Error = "invalid_token",
                        ErrorDescription = "Invalid access token"
                    };
                }
                
                // Extract subject (user ID) from claims
                var subClaim = claims.FirstOrDefault(c => c.Type == "sub");
                if (subClaim == null || !long.TryParse(subClaim.Value, out var userId))
                {
                    _logger.LogWarning("Missing subject claim in token");
                    return new UserInfoResult
                    {
                        IsSuccess = false,
                        Error = "invalid_token",
                        ErrorDescription = "Invalid token format"
                    };
                }
                
                // Get user
                var user = await _userRepo.GetUserById(userId);
                if (user == null)
                {
                    _logger.LogWarning("User not found for ID: {UserId}", userId);
                    return new UserInfoResult
                    {
                        IsSuccess = false,
                        Error = "invalid_token",
                        ErrorDescription = "User not found"
                    };
                }
                
                // Extract scope from claims
                var scopeClaim = claims.FirstOrDefault(c => c.Type == "scope");
                var scopes = scopeClaim?.Value.Split(' ') ?? new[] { "profile" };
                
                // Build user info based on requested scopes
                var userInfo = new Dictionary<string, object>
                {
                    ["sub"] = user.Id.ToString()
                };
                
                if (scopes.Contains("profile"))
                {
                    userInfo["name"] = $"{user.FirstName} {user.LastName}";
                    userInfo["given_name"] = user.FirstName;
                    userInfo["family_name"] = user.LastName;
                    userInfo["preferred_username"] = user.Username;
                }
                
                if (scopes.Contains("email"))
                {
                    userInfo["email"] = user.Email;
                    userInfo["email_verified"] = true; // Assuming email verification
                }
                
                if (scopes.Contains("phone"))
                {
                    userInfo["phone_number"] = user.PhoneNumber;
                    userInfo["phone_number_verified"] = user.TwoFactorEnabled; // If 2FA is enabled, phone is verified
                }
                
                _logger.LogInformation("User info returned for user: {Username}", user.Username);
                
                return new UserInfoResult
                {
                    IsSuccess = true,
                    UserInfo = userInfo
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user info");
                
                return new UserInfoResult
                {
                    IsSuccess = false,
                    Error = "server_error",
                    ErrorDescription = "An unexpected error occurred"
                };
            }
        }
        
        /// <summary>
        /// Gets a user by username
        /// </summary>
        public async Task<User> GetUserByUsernameAsync(string username)
        {
            return await _userRepo.GetByUsernameAsync(username);
        }

        /// <summary>
        /// Checks if a user can request a new OTP (rate limiting)
        /// </summary>
        public async Task<bool> CanRequestOtpAsync(string username)
        {
            // Rate limiting: Allow only 3 OTP requests in 15 minutes
            const int maxRequests = 3;
            const int timeWindowMinutes = 15;
            
            // Get current OTP requests from cache
            var otpRequests = await _cache.GetAsync<List<DateTime>>($"{OTP_REQUEST_PREFIX}{username}");
            if (otpRequests == null)
            {
                otpRequests = new List<DateTime>();
            }
            
            // Clean up old requests
            var cutoffTime = DateTime.UtcNow.AddMinutes(-timeWindowMinutes);
            otpRequests.RemoveAll(time => time < cutoffTime);
            
            // Check if user has exceeded rate limit
            if (otpRequests.Count >= maxRequests)
            {
                _logger.LogWarning("OTP rate limit exceeded for user: {Username}", username);
                //await _auditLogService.LogSecurityEventAsync("OTP_RATE_LIMIT_EXCEEDED", $"User: {username}");
                return false;
            }
            
            return true;
        }

        /// <summary>
        /// Logs an OTP request for rate limiting
        /// </summary>
        public async Task LogOtpRequestAsync(string username)
        {
            // Get current OTP requests from cache
            var otpRequests = await _cache.GetAsync<List<DateTime>>($"{OTP_REQUEST_PREFIX}{username}") 
                           ?? new List<DateTime>();
            
            // Add current request
            otpRequests.Add(DateTime.UtcNow);
            
            // Store updated list
            await _cache.SetAsync($"{OTP_REQUEST_PREFIX}{username}", otpRequests, TimeSpan.FromMinutes(15));
            
            //await _auditLogService.LogSecurityEventAsync("OTP_REQUESTED", $"User: {username}");
        }

        // Helper methods

        private void SetLoginPolicyLockDuration(LoginPolicy policy, TimeSpan duration)
        {
            // This should be properly implemented in the LoginPolicy entity class
            // For now, this is a mock implementation
            policy.SetLockStartDateTime(DateTime.UtcNow);
            policy.SetLockEndDateTime(DateTime.UtcNow.AddMinutes(10));
        }
        
        private string MaskPhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrEmpty(phoneNumber) || phoneNumber.Length <= 4)
                return "****";
                
            return $"****{phoneNumber.Substring(Math.Max(0, phoneNumber.Length - 4))}";
        }

        private async Task SaveOauthTokenAsync(string clientId, string userName, string accessToken, string refreshToken, short tokenType)
        {
            var oauthToken = new OauthToken(clientId, userName, accessToken, refreshToken, tokenType);
            await _oauthRepo.AddAsync(oauthToken);
            _logger.LogDebug("OAuth token saved for user: {Username}", userName);
        }

        private (bool IsValid, string ErrorMessage) ValidatePassword(string newPassword, string confirmPassword, ConfigurationPassword config)
        {
            // 1. Check if passwords match
            if (newPassword != confirmPassword)
            {
                return (false, "Passwords do not match");
            }

            // 2. Check password length
            if (newPassword.Length < config.MinPassLength || newPassword.Length > config.MaxPassLength)
            {
                return (false, $"Password must be between {config.MinPassLength} and {config.MaxPassLength} characters long");
            }

            // 3. Check if password contains at least one special character if required
            if (config.MustContainChar && !newPassword.Any(ch => !char.IsLetterOrDigit(ch)))
            {
                return (false, "Password must contain at least one special character");
            }

            // 4. Check for uppercase letters if required
            if (config.MustContainUpperCase && !newPassword.Any(char.IsUpper))
            {
                return (false, "Password must contain at least one uppercase letter");
            }

            // 5. Check if password is too numeric
            int numericCount = newPassword.Count(char.IsDigit);
            if (numericCount >= config.NumericPassNotEqual)
            {
                return (false, "Password cannot be mostly numeric");
            }

            // 6. Additional checks for complex passwords
            if (config.IsComplex)
            {
                // Check for lowercase
                if (!newPassword.Any(char.IsLower))
                {
                    return (false, "Password must contain at least one lowercase letter");
                }
                
                // Check for digits
                if (!newPassword.Any(char.IsDigit))
                {
                    return (false, "Password must contain at least one digit");
                }
            }

            return (true, string.Empty);
        }

        private string GenerateSecureAuthCode()
        {
            using var rng = RandomNumberGenerator.Create();
            var bytes = new byte[32]; // 256 bits of randomness
            rng.GetBytes(bytes);
            return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").Replace("=", "");
        }

        private bool VerifyPkceCodeVerifier(string codeVerifier, string codeChallenge, string codeChallengeMethod)
        {
            if (string.IsNullOrEmpty(codeVerifier) || string.IsNullOrEmpty(codeChallenge))
                return false;
                
            if (codeChallengeMethod == "S256")
            {
                using var sha256 = SHA256.Create();
                var challengeBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(codeVerifier));
                var computedChallenge = Convert.ToBase64String(challengeBytes)
                    .Replace("+", "-")
                    .Replace("/", "_")
                    .Replace("=", "");
                    
                return computedChallenge == codeChallenge;
            }
            
            // Plain method (not recommended)
            return codeVerifier == codeChallenge;
        }

        private string GetCurrentIpAddress()
        {
            return "127.0.0.1";
        }

        private bool IsIpAllowed(string clientIp, string allowedIpRanges)
        {
            // Basic implementation - in production, use proper CIDR matching
            if (string.IsNullOrEmpty(allowedIpRanges))
                return true; // No restrictions
                
            var ranges = allowedIpRanges.Split(',');
            return ranges.Any(r => clientIp.StartsWith(r.Trim()));
        }

        private bool IsRedirectUriValid(string allowedRedirectUris, string redirectUri)
        {
            if (string.IsNullOrEmpty(allowedRedirectUris) || string.IsNullOrEmpty(redirectUri))
                return false;
                
            var uriList = allowedRedirectUris.Split(',').Select(u => u.Trim());
            return uriList.Any(uri => uri == redirectUri);
        }

        private bool SecureCompare(string a, string b)
        {
            if (a == null || b == null || a.Length != b.Length)
                return false;
                
            return CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(a),
                Encoding.UTF8.GetBytes(b));
        }
        



    public class AuthCodeInfo
    {
        public string ClientId { get; set; }
        public string RedirectUri { get; set; }
        public string Scope { get; set; }
        public string State { get; set; }
        public string Nonce { get; set; }
        public string CodeChallenge { get; set; }
        public string CodeChallengeMethod { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
    }
    
    public class PasswordResetInfo
    {
        public string Username { get; set; }
        public DateTime ExpiresAt { get; set; }
    }

    public class AuthorizationResult
    {
        public bool IsSuccess { get; set; }
        public string AuthorizationCode { get; set; }
        public string State { get; set; }
        public bool RequiresCaptcha { get; set; }
        public string CaptchaToken { get; set; }
        public string Error { get; set; }
        public string ErrorDescription { get; set; }
    }
    
    public class TokenResult
    {
        public bool IsSuccess { get; set; }
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public string TokenType { get; set; }
        public int ExpiresIn { get; set; }
        public string Scope { get; set; }
        public string IdToken { get; set; }
        public string Error { get; set; }
        public string ErrorDescription { get; set; }
    }
    
    public class UserInfoResult
    {
        public bool IsSuccess { get; set; }
        public Dictionary<string, object> UserInfo { get; set; }
        public string Error { get; set; }
        public string ErrorDescription { get; set; }
    }

    public class PassResult 
    {
        public bool Success { get; set; }
        public string Password { get; set; }
        public string Message { get; set; }
        public string Error { get; set; }
    }

    public class AuthResult
    {
        public bool Success { get; set; }
        public string Token { get; set; }
        public string RefreshToken { get; set; }
        public string Message { get; set; }
        public string Error { get; set; }
        public bool TwoFactorRequired { get; set; }
        public bool PasswordChangeRequired { get; set; }
        public int ExpiresIn { get; set; }
        public string? TokenType { get; set; }
        public string? Scope { get; set; }
        public string? PendingToken { get; set; }
        public string? PhoneHint { get; set; }
        public int LockDuration { get; set; }
        public int AttemptsRemaining { get; set; }
    }
    
    public class ResetResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string Error { get; set; }
    }
}