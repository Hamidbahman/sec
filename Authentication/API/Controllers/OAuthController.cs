using Authentication.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Security.Claims;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Authentication.API.Controllers
{
    [ApiController]
    [Route("api/oauth")]
    [EnableRateLimiting("auth")]
    public class OAuthController : ControllerBase
    {
        private readonly OAuthService _authService;
        private readonly OtpService _otpService;
        private readonly ILogger<OAuthController> _logger;

        public OAuthController(
            OAuthService authService, 
            OtpService otpService,
            ILogger<OAuthController> logger)
        {
            _authService = authService;
            _otpService = otpService;
            _logger = logger;
        }

        /// <summary>
        /// OAuth 2.1 authorization endpoint
        /// </summary>
        [HttpGet("authorize")]
        [ProducesResponseType(typeof(void), StatusCodes.Status302Found)]
        public async Task<IActionResult> Authorize(
            [FromQuery] string response_type,
            [FromQuery] string client_id,
            [FromQuery] string redirect_uri,
            [FromQuery] string scope,
            [FromQuery] string state,
            [FromQuery] string code_challenge,
            [FromQuery] string code_challenge_method,
            [FromQuery] string? nonce = null,
            [FromQuery] string? captcha_token = null)
        {
            try
            {
                var result = await _authService.AuthorizeAsync(
                    client_id,
                    response_type,
                    redirect_uri,
                    state,
                    scope,
                    nonce,
                    code_challenge,
                    code_challenge_method,
                    captcha_token);

                if (result.IsSuccess)
                {
                    // Successful authorization, redirect back to client
                    var redirectUrl = $"{redirect_uri}?code={result.AuthorizationCode}&state={result.State}";
                    return Redirect(redirectUrl);
                }
                
                if (result.RequiresCaptcha)
                {
                    // Redirect to CAPTCHA page
                    return RedirectToAction("ShowCaptcha", new 
                    { 
                        token = result.CaptchaToken,
                        client_id = client_id, 
                        redirect_uri = redirect_uri,
                        state = state,
                        scope = scope,
                        response_type = response_type,
                        code_challenge = code_challenge,
                        code_challenge_method = code_challenge_method,
                        nonce = nonce
                    });
                }
                
                // Failed authorization, redirect with error
                var errorUrl = $"{redirect_uri}?error={result.Error}&error_description={WebUtility.UrlEncode(result.ErrorDescription)}&state={state}";
                return Redirect(errorUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during authorization");
                
                var errorUrl = $"{redirect_uri}?error=server_error&error_description={WebUtility.UrlEncode("An unexpected error occurred")}&state={state}";
                return Redirect(errorUrl);
            }
        }
        
        /// <summary>
        /// Show CAPTCHA challenge page
        /// </summary>
        [HttpGet("captcha")]
        [ProducesResponseType(typeof(void), StatusCodes.Status200OK)]
        public IActionResult ShowCaptcha(
            [FromQuery] string token,
            [FromQuery] string client_id,
            [FromQuery] string redirect_uri,
            [FromQuery] string state,
            [FromQuery] string scope,
            [FromQuery] string response_type,
            [FromQuery] string code_challenge,
            [FromQuery] string code_challenge_method,
            [FromQuery] string? nonce = null)
        {

            return Ok(new
            {
                Message = "CAPTCHA verification required",
                CaptchaToken = token,
                RedirectUri = redirect_uri,
                ClientId = client_id,
                State = state
            });
        }

        /// <summary>
        /// OAuth 2.1 token endpoint
        /// </summary>
        [HttpPost("token")]
        [Consumes("application/x-www-form-urlencoded")]
        [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(TokenErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(TokenErrorResponse), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Token(
            [FromForm] string grant_type,
            [FromForm] string? code = null,
            [FromForm] string? redirect_uri = null,
            [FromForm] string? client_id = null,
            [FromForm] string? client_secret = null,
            [FromForm] string? refresh_token = null,
            [FromForm] string? code_verifier = null,
            [FromForm] string? username = null,
            [FromForm] string? password = null,
            [FromForm] string? scope = null)
        {
            try
            {

                switch (grant_type)
                {
                    case "authorization_code":
                        if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(redirect_uri) || 
                            string.IsNullOrEmpty(client_id) || string.IsNullOrEmpty(client_secret) ||
                            string.IsNullOrEmpty(code_verifier))
                        {
                            return BadRequest(new TokenErrorResponse
                            {
                                Error = "invalid_request",
                                ErrorDescription = "Missing required parameters"
                            });
                        }
                        
                        var codeResult = await _authService.ExchangeCodeAsync(
                            code, client_id, client_secret, redirect_uri, code_verifier);
                            
                        if (codeResult.IsSuccess)
                        {
                            return Ok(new TokenResponse
                            {
                                AccessToken = codeResult.AccessToken,
                                RefreshToken = codeResult.RefreshToken,
                                TokenType = codeResult.TokenType,
                                ExpiresIn = codeResult.ExpiresIn,
                                Scope = codeResult.Scope
                            });
                        }
                        
                        return TokenError(codeResult.Error, codeResult.ErrorDescription);
                        
                    case "refresh_token":
                        if (string.IsNullOrEmpty(refresh_token) || string.IsNullOrEmpty(client_id) || 
                            string.IsNullOrEmpty(client_secret))
                        {
                            return BadRequest(new TokenErrorResponse
                            {
                                Error = "invalid_request",
                                ErrorDescription = "Missing required parameters"
                            });
                        }
                        
                        var refreshResult = await _authService.RefreshTokenAsync(
                            refresh_token, client_id, client_secret);
                            
                        if (refreshResult.IsSuccess)
                        {
                            return Ok(new TokenResponse
                            {
                                AccessToken = refreshResult.AccessToken,
                                RefreshToken = refreshResult.RefreshToken,
                                TokenType = refreshResult.TokenType,
                                ExpiresIn = refreshResult.ExpiresIn,
                                Scope = refreshResult.Scope
                            });
                        }
                        
                        return TokenError(refreshResult.Error, refreshResult.ErrorDescription);
                        
                    case "password":
                        return BadRequest(new TokenErrorResponse
                        {
                            Error = "unsupported_grant_type",
                            ErrorDescription = "Password grant type is not supported for security reasons"
                        });
                        
                    case "client_credentials":
                        return BadRequest(new TokenErrorResponse
                        {
                            Error = "unsupported_grant_type",
                            ErrorDescription = "Client credentials grant type is not supported yet"
                        });
                        
                    default:
                        return BadRequest(new TokenErrorResponse
                        {
                            Error = "unsupported_grant_type",
                            ErrorDescription = "Unsupported grant type"
                        });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during token request");
                
                return StatusCode(StatusCodes.Status500InternalServerError, new TokenErrorResponse
                {
                    Error = "server_error",
                    ErrorDescription = "An unexpected error occurred"
                });
            }
        }

        [HttpPost("revoke")]
        [Consumes("application/x-www-form-urlencoded")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(TokenErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Revoke(
            [FromForm] string token,
            [FromForm] string client_id,
            [FromForm] string client_secret,
            [FromForm] string? token_type_hint = null)
        {
            try
            {
                var success = await _authService.RevokeTokenAsync(token, client_id, client_secret, token_type_hint);

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during token revocation");
                
                return StatusCode(StatusCodes.Status500InternalServerError, new TokenErrorResponse
                {
                    Error = "server_error",
                    ErrorDescription = "An unexpected error occurred"
                });
            }
        }
        

        [HttpGet("userinfo")]
        [Authorize]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(TokenErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(TokenErrorResponse), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> UserInfo()
        {
            try
            {

                var authHeader = Request.Headers.Authorization.FirstOrDefault();
                if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
                {
                    return Unauthorized(new TokenErrorResponse
                    {
                        Error = "invalid_token",
                        ErrorDescription = "Missing or invalid token"
                    });
                }
                
                var token = authHeader["Bearer ".Length..];
                
                var result = await _authService.GetUserInfoAsync(token);
                
                if (result.IsSuccess)
                {
                    return Ok(result.UserInfo);
                }
                
                return Unauthorized(new TokenErrorResponse
                {
                    Error = result.Error,
                    ErrorDescription = result.ErrorDescription
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user info");
                
                return StatusCode(StatusCodes.Status500InternalServerError, new TokenErrorResponse
                {
                    Error = "server_error",
                    ErrorDescription = "An unexpected error occurred"
                });
            }
        }
        

        [HttpPost("login")]
        [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(TokenErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(TokenErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(TokenErrorResponse), StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new TokenErrorResponse
                    {
                        Error = "invalid_request",
                        ErrorDescription = "Invalid request format"
                    });
                }

                var result = await _authService.LoginAsync(
                    request.Username, 
                    request.Password,
                    request.AccessToken);

                if (result.Success)
                {
                    return Ok(new TokenResponse
                    {
                        AccessToken = result.Token,
                        RefreshToken = result.RefreshToken,
                        ExpiresIn = result.ExpiresIn,
                        TokenType = result.TokenType,
                        Scope = result.Scope
                    });
                }

                if (result.TwoFactorRequired)
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new OtpRequiredResponse
                    { 
                        Error = "otp_required",
                        ErrorDescription = "OTP verification required",
                        PendingToken = result.PendingToken,
                        PhoneHint = result.PhoneHint
                    });
                }
                
                if (result.Error == "account_locked")
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new TokenErrorResponse
                    {
                        Error = result.Error,
                        ErrorDescription = result.Message,
                        LockDuration = result.LockDuration
                    });
                }

                return Unauthorized(new TokenErrorResponse
                {
                    Error = result.Error ?? "invalid_grant",
                    ErrorDescription = result.Message,
                    AttemptsRemaining = result.AttemptsRemaining
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login attempt");
                
                return StatusCode(StatusCodes.Status500InternalServerError, new TokenErrorResponse
                {
                    Error = "server_error",
                    ErrorDescription = "An unexpected error occurred"
                });
            }
        }


        [HttpPost("verify-otp")]
        [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(TokenErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(TokenErrorResponse), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> VerifyOtp([FromBody] OtpRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new TokenErrorResponse
                    {
                        Error = "invalid_request",
                        ErrorDescription = "Invalid request format"
                    });
                }
                
                var result = await _authService.VerifyOtpAsync(
                    request.Username, 
                    request.OtpCode,
                    request.PendingToken);
                
                if (result.Success)
                {
                    return Ok(new TokenResponse
                    {
                        AccessToken = result.Token,
                        RefreshToken = result.RefreshToken,
                        ExpiresIn = result.ExpiresIn,
                        TokenType = result.TokenType,
                        Scope = result.Scope
                    });
                }

                if (result.PasswordChangeRequired)
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new TokenErrorResponse
                    {
                        Error = "password_expired",
                        ErrorDescription = result.Message,
                        PasswordChangeRequired = true
                    });
                }

                if (result.TwoFactorRequired)
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new OtpRequiredResponse
                    {
                        Error = "invalid_otp",
                        ErrorDescription = result.Message,
                        PendingToken = result.PendingToken
                    });
                }

                return Unauthorized(new TokenErrorResponse
                {
                    Error = result.Error ?? "invalid_grant",
                    ErrorDescription = result.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during OTP verification");
                
                return StatusCode(StatusCodes.Status500InternalServerError, new TokenErrorResponse
                {
                    Error = "server_error",
                    ErrorDescription = "An unexpected error occurred"
                });
            }
        }

        [HttpPost("resend-otp")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(TokenErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(TokenErrorResponse), StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> ResendOtp([FromBody] ResendOtpRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.Username))
                {
                    return BadRequest(new TokenErrorResponse
                    {
                        Error = "invalid_request",
                        ErrorDescription = "Username is required"
                    });
                }

                var user = await _authService.GetUserByUsernameAsync(request.Username);
                if (user == null)
                {
                    // Don't reveal if user exists
                    return Ok(new SuccessResponse
                    {
                        Message = "If your account exists, an OTP has been sent."
                    });
                }

                // Check rate limiting for OTP requests to prevent abuse
                if (!await _authService.CanRequestOtpAsync(request.Username))
                {
                    return StatusCode(StatusCodes.Status429TooManyRequests, new TokenErrorResponse
                    {
                        Error = "too_many_requests",
                        ErrorDescription = "Too many OTP requests. Please try again later."
                    });
                }

                // Generate and send new OTP
                var otpCode =  _otpService.GenerateOtp(user.Id);
                 _otpService.SendOtp(user.PhoneNumber, otpCode);
                
                await _authService.LogOtpRequestAsync(request.Username);

                _logger.LogInformation("OTP resent for user: {Username}", request.Username);

                return Ok(new SuccessResponse
                { 
                    Message = "A new verification code has been sent to your phone.",
                    AdditionalInfo = $"Sent to number ending in {user.PhoneNumber.Substring(Math.Max(0, user.PhoneNumber.Length - 4))}"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resending OTP");
                
                return StatusCode(StatusCodes.Status500InternalServerError, new TokenErrorResponse
                {
                    Error = "server_error",
                    ErrorDescription = "An unexpected error occurred"
                });
            }
        }

        [HttpPost("change-password")]
        [Authorize]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(TokenErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new TokenErrorResponse
                    {
                        Error = "invalid_request",
                        ErrorDescription = "Invalid request format"
                    });
                }

                var result = await _authService.ChangePassword(
                    request.Username, 
                    request.CurrentPassword, 
                    request.NewPassword, 
                    request.ConfirmPassword);

                if (!result.Success)
                {
                    return BadRequest(new TokenErrorResponse
                    {
                        Error = result.Error ?? "invalid_request",
                        ErrorDescription = result.Message
                    });
                }

                return Ok(new SuccessResponse
                {
                    Message = result.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password for user: {Username}", request.Username);
                
                return StatusCode(StatusCodes.Status500InternalServerError, new TokenErrorResponse
                {
                    Error = "server_error",
                    ErrorDescription = "An unexpected error occurred"
                });
            }
        }


        [HttpPost("reset-password/initiate")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(TokenErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> InitiatePasswordReset([FromBody] ResetPasswordInitiateRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.Username))
                {
                    return BadRequest(new TokenErrorResponse
                    {
                        Error = "invalid_request",
                        ErrorDescription = "Username is required"
                    });
                }
                
                var result = await _authService.InitiatePasswordResetAsync(request.Username);
                
                // Always return 200 OK to prevent username enumeration
                return Ok(new SuccessResponse
                {
                    Message = result.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initiating password reset");
                
                // Still return 200 OK to prevent username enumeration
                return Ok(new SuccessResponse
                {
                    Message = "If your account exists, a password reset link has been sent to your email."
                });
            }
        }
        


        [HttpPost("reset-password/complete")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(TokenErrorResponse),StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CompletePasswordReset([FromBody] ResetPasswordCompleteRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.ResetToken) || 
                    string.IsNullOrEmpty(request.NewPassword) || 
                    string.IsNullOrEmpty(request.ConfirmPassword))
                {
                    return BadRequest(new TokenErrorResponse
                    {
                        Error = "invalid_request",
                        ErrorDescription = "Reset token and new password are required"
                    });
                }
                
                var result = await _authService.CompletePasswordResetAsync(
                    request.ResetToken, 
                    request.NewPassword, 
                    request.ConfirmPassword);
                
                if (!result.Success)
                {
                    return BadRequest(new TokenErrorResponse
                    {
                        Error = result.Error ?? "invalid_request",
                        ErrorDescription = result.Message
                    });
                }
                
                return Ok(new SuccessResponse
                {
                    Message = result.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing password reset");
                
                return StatusCode(StatusCodes.Status500InternalServerError, new TokenErrorResponse
                {
                    Error = "server_error",
                    ErrorDescription = "An unexpected error occurred"
                });
            }
        }


        [HttpGet(".well-known/openid-configuration")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        public IActionResult OpenIdConfiguration()
        {
            var baseUrl = $"{Request.Scheme}://{Request.Host}{Request.PathBase}";
            
            return Ok(new
            {
                issuer = $"{baseUrl}",
                authorization_endpoint = $"{baseUrl}/api/oauth/authorize",
                token_endpoint = $"{baseUrl}/api/oauth/token",
                userinfo_endpoint = $"{baseUrl}/api/oauth/userinfo",
                jwks_uri = $"{baseUrl}/api/oauth/.well-known/jwks.json",
                response_types_supported = new[] { "code" },
                subject_types_supported = new[] { "public" },
                id_token_signing_alg_values_supported = new[] { "RS256" },
                scopes_supported = new[] { "openid", "profile", "email", "phone" },
                token_endpoint_auth_methods_supported = new[] { "client_secret_post" },
                claims_supported = new[] { "sub", "name", "preferred_username", "email", "email_verified", "phone_number" },
                code_challenge_methods_supported = new[] { "S256" },
                grant_types_supported = new[] { "authorization_code", "refresh_token" }
            });
        }
        


        [HttpGet("health")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(HealthResponse), StatusCodes.Status200OK)]
        public IActionResult Health()
        {
            return Ok(new HealthResponse 
            { 
                Status = "Healthy",
                Version = "1.0.0",
                Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"
            });
        }
        

        private IActionResult TokenError(string error, string description)
        {
            if (error == "invalid_client")
            {
                return StatusCode(StatusCodes.Status401Unauthorized, new TokenErrorResponse
                {
                    Error = error,
                    ErrorDescription = description
                });
            }
            
            return BadRequest(new TokenErrorResponse
            {
                Error = error,
                ErrorDescription = description
            });
        }
    }

    #region Request Models

    public class LoginRequest
    {
        [Required]
        public string Username { get; set; }
        
        [Required]
        public string Password { get; set; }
        
        [Required]
        public string AccessToken { get; set; }
    }

    public class OtpRequest
    {
        [Required]
        public string Username { get; set; }
        
        [Required]
        public string OtpCode { get; set; }
        
        [Required]
        public string PendingToken { get; set; }
    }

    public class ChangePasswordRequest
    {
        [Required]
        public string Username { get; set; }
        
        [Required]
        public string CurrentPassword { get; set; }
        
        [Required]
        [MinLength(8)]
        public string NewPassword { get; set; }
        
        [Required]
        [Compare("NewPassword")]
        public string ConfirmPassword { get; set; }
    }

    public class ResendOtpRequest
    {
        [Required]
        public string Username { get; set; }
    }
    
    public class ResetPasswordInitiateRequest
    {
        [Required]
        public string Username { get; set; }
    }
    
    public class ResetPasswordCompleteRequest
    {
        [Required]
        public string ResetToken { get; set; }
        
        [Required]
        [MinLength(8)]
        public string NewPassword { get; set; }
        
        [Required]
        [Compare("NewPassword")]
        public string ConfirmPassword { get; set; }
    }

    #endregion

    #region Response Models

    public class TokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; }
        
        [JsonPropertyName("token_type")]
        public string TokenType { get; set; }
        
        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }
        
        [JsonPropertyName("refresh_token")]
        public string RefreshToken { get; set; }
        
        [JsonPropertyName("scope")]
        public string Scope { get; set; }
        
        [JsonPropertyName("id_token")]
        public string IdToken { get; set; }
    }

    public class TokenErrorResponse
    {
        [JsonPropertyName("error")]
        public string Error { get; set; }
        
        [JsonPropertyName("error_description")]
        public string ErrorDescription { get; set; }
        
        [JsonPropertyName("password_change_required")]
        public bool PasswordChangeRequired { get; set; }
        
        [JsonPropertyName("lock_duration")]
        public int? LockDuration { get; set; }
        
        [JsonPropertyName("attempts_remaining")]
        public int? AttemptsRemaining { get; set; }
    }
    
    public class OtpRequiredResponse : TokenErrorResponse
    {
        [JsonPropertyName("pending_token")]
        public string PendingToken { get; set; }
        
        [JsonPropertyName("phone_hint")]
        public string PhoneHint { get; set; }
    }

    public class SuccessResponse
    {
        [JsonPropertyName("message")]
        public string Message { get; set; }
        
        [JsonPropertyName("additional_info")]
        public string AdditionalInfo { get; set; }
    }

    public class HealthResponse
    {
        public string Status { get; set; }
        public string Version { get; set; }
        public string Environment { get; set; }
    }

    #endregion
}
