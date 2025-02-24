using System;
using System.Collections.Concurrent;
using System.Data.Common;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Authentication.Domain.Entities;
using Authentication.Domain.Enums;
using Authentication.Domain.Repositories;

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

public async Task<string> LoginAsync(string username, string password, string authenticationCode, string otprecieved, string phoneNumber)
{
    var user = await _userRepo.GetByUsernameAsync(username);
    if (user == null)
        throw new AuthenticationException("No user found");

    // var loginPolicy = await _userRepo.GetLoginPoliciesByUserID(user.Id.ToString());
    // if (loginPolicy != null && loginPolicy.LockTypes != LockTypes.None)
    // {
    //     string lockMessage = loginPolicy.LockTypes switch
    //     {
    //         LockTypes.TemporaryLock => "Your account is temporarily locked. Please try again later.",
    //         LockTypes.PermanentLock => "Your account has been permanently locked. Contact support.",
    //         LockTypes.ExpiringLock => "Your account is locked and will expire soon.",
    //         LockTypes.ConditionalLock => "Your account is locked due to policy restrictions.",
    //         _ => "Your account is locked."
    //     };
    //     if(!string.IsNullOrEmpty(lockMessage))
    //         throw new AuthenticationException(lockMessage);
    // }


    if (password != user.UserProperty.Password)
    {
        throw new AuthenticationException("password is in correct");
    }
        user.IncrementLoginAttempt();
        _userRepo.SaveChangesAsync();



    // Validate authentication code from stored dictionary
    // Validate the authentication code received from the user
    if (!_authCodes.TryGetValue(authenticationCode, out _))
                throw new AuthenticationException("Invalid or expired authentication code");

// Remove used authentication code to prevent reuse
_authCodes.TryRemove(authenticationCode, out _);

// The code is valid now, proceed with your further logic



    if(user.TwoFactorEnabled == false)
        return GenerateAccessToken(user);


    // Validate OTP if necessary
    string  otpGenerate =  await _otpService.GenerateOTPAsync(phoneNumber, 6);
     _otpService.ValidateOTP(otprecieved, otpGenerate);


    // Generate access token after successful validation
    var accessToken = GenerateAccessToken(user);

    return accessToken;
}

// Mocked method for access token generation (replace with real implementation)
private string GenerateAccessToken(User user)
{
    return Convert.ToBase64String(Encoding.UTF8.GetBytes($"{user.Id}:{Guid.NewGuid()}"));
}


    }
}