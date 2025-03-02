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
            [FromQuery] string? state = null,
            [FromQuery] string? scope = null,
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
                        response_type = response_type
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
            [FromQuery] string? state = null,
            [FromQuery] string? scope = null,
            [FromQuery] string? response_type = null)
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
                            string.IsNullOrEmpty(client_id) || string.IsNullOrEmpty(client_secret))
                        {
                            return BadRequest(new TokenErrorResponse
                            {
                                Error = "invalid_request",
                                ErrorDescription = "Missing required parameters"
                            });
                        }
                        
                        var codeResult = await _authService.ExchangeCodeAsync(
                            code, client_id, client_secret, redirect_uri);
                            
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
        
        // ... (rest of the code remains the same as in the previous file)
    }

    // ... (rest of the classes remain the same)
}