using Authentication.Application;
using Microsoft.AspNetCore.Mvc;


namespace Auhtentication.Application;
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly OAuthService _authService;

    public AuthController(OAuthService authService)
    {
        _authService = authService;
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
}

public class LoginRequest
{
    public string Username { get; set; }
    public string Password { get; set; }
    public string AuthenticationCode {get;set;}
}

public class OtpRequest
{
    public string Username { get; set; }
    public string OtpCode { get; set; }
}
