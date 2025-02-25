using System;
using System.Collections.Concurrent;
using System.Security.Authentication;
using System.Text;
using System.Threading.Tasks;
using Application;
using Authentication.Domain.Entities;
using Authentication.Domain.Repositories;
using Domain.Repositories;

namespace Authentication.Application
{
    public class OAuthService
    {
        private readonly IApplicationRepository _applicationRepository;
        private readonly IUserPropertyRepository _userPropertyRepo;
        private readonly IUserRepository _userRepo;
        private readonly OtpService _otpService;
        private readonly CheckboxCaptchaService _checkBox;
        private readonly TokenService _tokenService;
        private readonly PuzzleCaptchaService _puzzleService;
        private static readonly ConcurrentDictionary<string, string> _authCodes = new();

        public OAuthService(
            PuzzleCaptchaService puzzleCaptchaService,
            IUserPropertyRepository userPropertyRepository,
            TokenService tokenService,
            CheckboxCaptchaService checkboxCaptchaService,
            OtpService otpService,
            IApplicationRepository applicationRepository,
            IUserRepository userRepository)
        {
            _puzzleService = puzzleCaptchaService;
            _userPropertyRepo = userPropertyRepository;
            _tokenService = tokenService;
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
                throw new AuthenticationException("Account is locked");
            }

            await _userRepo.SaveChangesAsync();

            if (!user.TwoFactorEnabled)
            {
                _authCodes.TryRemove(authenticationCode, out _);
                var token = _tokenService.GenerateAccessToken(user.Id);

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

    var confPass = await _userPropertyRepo.GetConfigurationPasswordByUserIdAsync(user.UserProperty.ConfigurationPasswordId);

    if (confPass == null)
    {
        return new AuthResult
        {
            Success = false,
            Message = "User password configuration not found",
            TwoFactorRequired = false
        };
    }
    
    var expirationD = confPass.CreateDate.AddDays(confPass.ExpireDaysAmount);
    // Check if the password is expired
    if (expirationD <= DateTime.UtcNow)  // Assuming ExpirationDate is a DateTime field
    {
        return new AuthResult
        {
            Success = false,
            Message = "Password expired. Please change your password.",
            TwoFactorRequired = false
        };
    }

    // Validate OTP
    if (_otpService.ValidateOtp(user.Id, otpCode))
    {
        var token = _tokenService.GenerateAccessToken(user.Id);
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

public async Task<PassResult> ChangePassword(string username, string exPassword, string newPassword, string confirmPassword)
{
    var user = await _userRepo.GetByUsernameAsync(username);
    if (user == null)
        throw new Exception("No user found");

    // Check if old password matches
    if (exPassword != user.UserProperty.Password)
    {
        return new PassResult
        {
            Success = false,
            Password = null,
            Message = "Password is incorrect"
        };
    }

    // Get password configuration
    var confPass = await _userPropertyRepo.GetConfigurationPasswordByUserIdAsync(user.UserProperty.ConfigurationPasswordId);
    if (confPass == null)
    {
        throw new Exception("Configuration settings not found");
    }

    // 1. Check if newPassword and confirmPassword match
    if (newPassword != confirmPassword)
    {
        return new PassResult
        {
            Success = false,
            Password = null,
            Message = "Passwords do not match"
        };
    }

    // 2. Check password length
    if (newPassword.Length < confPass.MinPassLength || newPassword.Length > confPass.MaxPassLength)
    {
        return new PassResult
        {
            Success = false,
            Password = null,
            Message = $"Password must be between {confPass.MinPassLength} and {confPass.MaxPassLength} characters long."
        };
    }

    // 3. Check if password contains at least one special character
    if (confPass.MustContainChar && !newPassword.Any(ch => !char.IsLetterOrDigit(ch)))
    {
        return new PassResult
        {
            Success = false,
            Password = null,
            Message = "Password must contain at least one special character."
        };
    }

    if (confPass.MustContainUpperCase == true && !newPassword.Any(char.IsUpper))
    {
        return new PassResult
        {
            Success = false,
            Password = null,
            Message = "Password must contain at least one uppercase letter."
        };
    }

    // 5. Check if password is too numeric
    int numericCount = newPassword.Count(char.IsDigit);
    if (numericCount >= confPass.NumericPassNotEqual)
    {
        return new PassResult
        {
            Success = false,
            Password = null,
            Message = "Password cannot be mostly numeric."
        };
    }

    // 6. If all checks pass, update password
    user.UserProperty.SetPassowrd(newPassword);
    await _userPropertyRepo.SaveChangesAsync(); // You may need a setter method in your entity
    await _userRepo.SaveChangesAsync();

    return new PassResult
    {
        Success = true,
        Password = newPassword,
        Message = "Password changed successfully"
    };
}

  
    }

    public class PassResult 
    {
        public bool Success {get;set;}
        public string Password {get;set;}
        public string? Message {get;set;}
    }

    public class AuthResult
    {
        public bool Success { get; set; }
        public string? Token { get; set; }
        public string? Message { get; set; }
        public bool TwoFactorRequired { get; set; }
    }
}
