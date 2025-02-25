using Authentication.Application;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Authentication.Application
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly OAuthService _authService;

        public AuthController(OAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("generate-auth-code")]
        public async Task<IActionResult> GenerateAuthCode([FromBody] AuthCodeRequest request)
        {
            var authCode = await _authService.GenerateAuthorizationCodeAsync(request.ClientId, request.ClientSecret, request.UserCaptchaToken);

            if (authCode == null) return Unauthorized(new { Message = "Invalid client credentials." });
            if (authCode == "InvalidCaptcha") return BadRequest(new { Message = "Invalid Captcha." });

            return Ok(new { AuthorizationCode = authCode });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var result = await _authService.LoginAsync(request.Username, request.Password, request.AuthenticationCode);
            if (result.Success) return Ok(new { Token = result.Token });
            if (result.TwoFactorRequired) return Unauthorized(new { Message = "OTP required." });

            return Unauthorized(new { Message = result.Message });
        }

        [HttpPost("verify-otp")]
        public async Task<IActionResult> VerifyOtp([FromBody] OtpRequest request)
        {
            var result = await _authService.VerifyOtpAsync(request.Username, request.OtpCode);
            if (result.Success) return Ok(new { Token = result.Token });

            return Unauthorized(new { Message = result.Message });
        }


    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest model)
    {
        if (model == null)
            return BadRequest("Invalid request.");

        try
        {
            var result = await _authService.ChangePassword(model.Username, model.ExPassword, model.NewPassword, model.ConfirmPassword);

            if (!result.Success)
                return BadRequest(new { result.Message });

            return Ok(new { result.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Message = "An error occurred.", Error = ex.Message });
        }
    }
}

public class ChangePasswordRequest
{
    public string Username { get; set; }
    public string ExPassword { get; set; }
    public string NewPassword { get; set; }
    public string ConfirmPassword { get; set; }
}

    }

    public class AuthCodeRequest
    {
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string? UserCaptchaToken { get; set; }
    }

    public class LoginRequest
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string AuthenticationCode { get; set; }
    }

    public class OtpRequest
    {
        public string Username { get; set; }
        public string OtpCode { get; set; }
    }

