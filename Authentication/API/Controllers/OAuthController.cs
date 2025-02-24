using System;
using System.Security.Authentication;
using System.Threading.Tasks;
using Authentication.Application;
using Authentication.Domain.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Authentication.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OAuthController : ControllerBase
    {
        private readonly OAuthService _oAuthService;
        private readonly OTPService _otp;

        public OAuthController(OTPService Otp,OAuthService oAuthService)
        {
            _oAuthService = oAuthService;
            _otp = Otp;
        }

        /// <summary>
        /// Generates an authorization code.
        /// </summary>
        [HttpPost("generate-auth-code")]
        public async Task<IActionResult> GenerateAuthorizationCode([FromBody] AuthCodeRequest request)
        {
            var authCode = await _oAuthService.GenerateAuthorizationCodeAsync(
                request.ClientId, 
                request.ClientSecret, 
                request.CaptchaToken
            );

            if (authCode == null)
                return Unauthorized("Invalid client credentials");

            if (authCode == "InvalidCaptcha")
                return BadRequest("Captcha validation failed");

            return Ok(new { AuthorizationCode = authCode });
        }

        /// <summary>
        /// Logs in a user.
        /// </summary>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                var accessToken = await _oAuthService.LoginAsync(
                    request.Username, 
                    request.Password, 
                    request.AuthenticationCode

                );

                return Ok(new { AccessToken = accessToken });
            }
            catch (AuthenticationException ex)
            {
                return Unauthorized(new { Error = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }
    

        [HttpPost("login-with-2fa")]
        public async Task<string> Two_Factort_Login([FromBody] TwoFactorRequest request)
        {
            var optGenerate = await _otp.GenerateOTPAsync(request.PhoneNumber, 6);
            return optGenerate;
        }

        [HttpPost("login_2FA")]
        public async Task<string> TwoFactorToken([FromBody] OtpVal otpval)
        {
            var val =  _otp.ValidateOTP(otpval.OtpGenerate, otpval.PhoneNumber);
            return null;
        }
    }


    /// <summary>
    /// Request model for generating an authorization code.
    /// </summary>
    public class AuthCodeRequest
    {
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string? CaptchaToken { get; set; }
    }

    /// <summary>
    /// Request model for login.
    /// </summary>
    public class LoginRequest
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string AuthenticationCode { get; set; }

    }

    public class TwoFactorRequest
    {
        public string PhoneNumber {get;set;}
    }
    
    public class OtpVal
    {
        public string OtpGenerate {get;set;}
        public string PhoneNumber {get;set;}
    }
}
