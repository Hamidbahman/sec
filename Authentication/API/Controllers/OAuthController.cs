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
        private readonly OtpService _smsService;

        public AuthController(OAuthService authService, OtpService smsService)
        {
            _smsService = smsService;
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


        
[HttpPost("validate-otp")]
public IActionResult ValidateOtp([FromBody] OtpValidationRequest request)
{
    bool isValid = _smsService.ValidateOtp(request.PhoneNumber, request.Otp);

    if (!isValid)
    {
        return BadRequest(new
        {
            success = false,
            message = "Invalid or expired OTP"
        });
    }

    return Ok(new
    {
        success = true,
        message = "OTP validated successfully"
    });
}



[HttpPost("send-otp")]
public async Task<IActionResult> SendOtp([FromBody] OtpRequest request)
{
    bool success = await _smsService.SendSmsAsync(request.PhoneNumber);

    if (!success)
    {
        return BadRequest(new
        {
            success = false,
            message = "Failed to send OTP"
        });
    }

    return Ok(new
    {
        success = true,
        message = "OTP sent successfully",
        expiresInMinutes = 5
    });
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

public class TestOtpRequest
{
    public string PhoneNumber { get; set; }
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
        public string PhoneNumber { get; set; }
        public string OtpCode { get; set; }
    }

    public class OtpValidationRequest
    {
        public string Otp {get;set;}
        public string PhoneNumber {get;set;}
    }

