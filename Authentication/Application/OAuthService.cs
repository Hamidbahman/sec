using System;
using System.Collections.Concurrent;
using System.Data.Common;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;


namespace Authentication.Application
{
    public class OAuthService
    {
        private readonly IApplicationRepository _applicationRepository;
        private readonly IUserRepository _userRepo;
        private readonly OTPService _otpService;
        private readonly OTPService _smsService;
        private readonly CheckboxCaptchaService _checkBox;
        private static readonly ConcurrentDictionary<string, string> _authCodes = new();

        public OAuthService(
            CheckboxCaptchaService checkboxCaptchaService,
            OTPService otpService,
            OTPService smsService,
            IApplicationRepository applicationRepository,
            IUserRepository userRepository)
        {
            _checkBox = checkboxCaptchaService;
            _applicationRepository = applicationRepository;
            _userRepo = userRepository;
            _otpService = otpService;
            _smsService = smsService;
        }

        public async Task<string?> GenerateAuthorizationCodeAsync(string clientId, string clientSecret, string? userCaptchaToken = null)
        {
            var application = await _applicationRepository.GetApplicationByClientIdAsync(clientId);
            if (application == null || clientSecret != application.ClientSecret)
                return null;

            var configLock = await _applicationRepository.GetConfigurationLockAsync(clientId);
            if (configLock.CaptchaNeeded)
            {
                if (string.IsNullOrEmpty(userCaptchaToken))
                    return _checkBox.GenerateCaptchaToken();

                if (!_checkBox.ValidateCaptchaToken(userCaptchaToken))
                    return "InvalidCaptcha";
            }

            string authCode = Guid.NewGuid().ToString();
            _authCodes.TryAdd(authCode, clientId);
            return authCode;
        }

public async Task<string> LoginAsync(string username, string password, string authenticationCode)
{
    var user = await _userRepo.GetByUsernameAsync(username);
    if (user == null || user.UerPropery.Password != password)
    {
        if(!_authCodes.ContainsKey(authenticationCode))
        {
        throw new AuthenticationException("Invalid Authentication code");
        }

        if(!user.TwoFactorEnabled)
        {


        _authCodes.TryRemove(authenticationCode, out _);
            var token = GenerateAccessToken(user);

            return new AuthResult {
                Success = false, Token = token
            };
        }
        }


        var otpCode = _otpService.GenerateOtp(user.Id);
        _otpService.SendOtpAsync(user.PhoneNumber, otpCode);

        return new AuthResult {
            Success = false, Message = "Otp Required", user.TwoFactorEnabled == true
        };



}

    public async Task<AuthResult> VerifyOtpAsync(string username, string otpCode)
    {
        var user = await _userRepo.GetByUsernameAsync(username);
        if (user == null)
            return new AuthResult{Success == false,  Message = "user not found"};

        if(_otpService.ValidateOtp(user.Id, otpCode))
        {
            var token = GenerateAccessToken(user);
            return new AuthResult{
                Success = true, Token =token
            };
        }
        return new AuthResult {
            Success = false, Message = "Invalid oTp"
        };
    }




private string GenerateAccessToken(User user)
{
    return Convert.ToBase64String(Encoding.UTF8.GetBytes($"{user.Id}:{Guid.NewGuid()}"));
}


    }
}

public class AutheResult
{
    public bool Success {get;set;}
    public string Token {get;set;}
    public string Message {get;set;}
    public bool TwoFactorRequired {get;set;}
}