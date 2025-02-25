using System;
using System.Collections.Concurrent;
using System.Security.Authentication;
using System.Text;
using System.Threading.Tasks;
using Authentication.Domain.Entities;
using Authentication.Domain.Repositories;

namespace Authentication.Application
{
    public class OAuthService
    {
        private readonly IApplicationRepository _applicationRepository;
        private readonly IUserRepository _userRepo;
        private readonly OtpService _otpService;
        private readonly CheckboxCaptchaService _checkBox;
        private static readonly ConcurrentDictionary<string, string> _authCodes = new();

        public OAuthService(
            CheckboxCaptchaService checkboxCaptchaService,
            OtpService otpService,
            IApplicationRepository applicationRepository,
            IUserRepository userRepository)
        {
            _checkBox = checkboxCaptchaService;
            _applicationRepository = applicationRepository;
            _userRepo = userRepository;
            _otpService = otpService;
        }

        public async Task<string?> GenerateAuthorizationCodeAsync(string clientId, string clientSecret, string? userCaptchaToken = null)
        {
            var failedAttempt = 0;
            var application = await _applicationRepository.GetApplicationByClientIdAsync(clientId);
            if (application == null || clientSecret != application.ClientSecret)
                return null;
            
            var configLock = await _applicationRepository.GetConfigurationLockAsync(clientId);
            failedAttempt ++;
            await _applicationRepository.SaveChangesAsync();
            if(failedAttempt >3)
            {
                configLock.EnableCaptcha();
            }

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

        public async Task<AuthResult> LoginAsync(string username, string password, string authenticationCode)
        {
            var user = await _userRepo.GetByUsernameAsync(username);
            
            // Validate authentication code
            if (!_authCodes.ContainsKey(authenticationCode))
            {
                throw new AuthenticationException("Invalid authentication code");
            }

            if (user == null || user.UserProperty.Password != password)
            {
                return new AuthResult
                {
                    Success = false,
                    Message = "Invalid username or password",
                    TwoFactorRequired = false
                };
            }
            user.IncrementLoginAttempt();
            var logPol = await _userRepo.GetLoginPoliciesByUserID(user.Id.ToString());
            if(logPol != null && user.LoginAttempt>5)
            {
                logPol.SetLockType (Domain.Enums.LockTypes.TemporaryLock);
            }

            await _userRepo.SaveChangesAsync();

            if (!user.TwoFactorEnabled)
            {
                _authCodes.TryRemove(authenticationCode, out _);
                var token = GenerateAccessToken(user);

                return new AuthResult
                {
                    Success = true,
                    Token = token,
                    TwoFactorRequired = false
                };
            }

            // Generate and send OTP
            var otpCode = _otpService.GenerateOtp(user.Id);
            _otpService.SendOtp(user.PhoneNumber, otpCode);

            return new AuthResult
            {
                Success = false,
                Message = "OTP Required",
                TwoFactorRequired = true
            };
        }

        public async Task<AuthResult> VerifyOtpAsync(string username, string otpCode)
        {
            var user = await _userRepo.GetByUsernameAsync(username);
            if (user == null)
            {
                return new AuthResult
                {
                    Success = false,
                    Message = "User not found",
                    TwoFactorRequired = false
                };
            }

            if (_otpService.ValidateOtp(user.Id, otpCode))
            {
                var token = GenerateAccessToken(user);
                return new AuthResult
                {
                    Success = true,
                    Token = token,
                    TwoFactorRequired = false
                };
            }

            return new AuthResult
            {
                Success = false,
                Message = "Invalid OTP",
                TwoFactorRequired = true
            };
        }

        private string GenerateAccessToken(User user)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes($"{user.Id}:{Guid.NewGuid()}"));
        }
    }

    public class AuthResult
    {
        public bool Success { get; set; }
        public string? Token { get; set; }
        public string? Message { get; set; }
        public bool TwoFactorRequired { get; set; }
    }
}
