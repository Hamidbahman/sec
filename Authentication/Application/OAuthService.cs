using System;
using System.Collections.Concurrent;
using System.Security.Authentication;
using System.Text;
using System.Threading.Tasks;
using Application;
using Authentication.Domain.Entities;
using Authentication.Domain.Repositories;
using Domain.Repositories;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Authentication.Application;

    public class OAuthService
    {
        private readonly IApplicationRepository _applicationRepository;
        private readonly IUserPropertyRepository _userPropertyRepo;
        private readonly IConfigurationSessionRepository _sessionRepo;
        private readonly IUserRepository _userRepo;
        private readonly OtpService _otpService;
        private readonly CheckboxCaptchaService _checkBox;
        private readonly TokenService _tokenService;
        private readonly PuzzleCaptchaService _puzzleService;
        private readonly IOAuthTokenRepository _OauthRepo;
        private static readonly ConcurrentDictionary<string, string> _authCodes = new();

        public OAuthService(
            IConfigurationSessionRepository configurationSessionRepository,
            IOAuthTokenRepository oauthRepo,
            PuzzleCaptchaService puzzleCaptchaService,
            IUserPropertyRepository userPropertyRepository,
            TokenService tokenService,
            CheckboxCaptchaService checkboxCaptchaService,
            OtpService otpService,

            IApplicationRepository applicationRepository,
            IUserRepository userRepository)
        {
            _sessionRepo = configurationSessionRepository;
            _OauthRepo = oauthRepo;
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

            var session = await _sessionRepo.GetByApplicationIdAsync(application.Id);

            if(!session.IsConcurrentActive)
            {
                throw new Exception("Application is not concurrently available");
            }
            
            if(application.ConfigurationSession.ConcurrencyCount > 100)
            {
                throw new Exception("Concurrency problem");
            }



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
    if (logPol != null && user.LoginAttempt > 5)
    {
        logPol.SetLockType(Domain.Enums.LockTypes.TemporaryLock);
        throw new AuthenticationException("Account is locked");
    }

    await _userRepo.SaveChangesAsync();

    if (!user.TwoFactorEnabled)
    {
        _authCodes.TryRemove(authenticationCode, out _);
        var accessToken = _tokenService.GenerateAccessToken(user.Id);
        var refreshToken = _tokenService.GenerateRefreshToken();
        
        await SaveOauthTokenAsync(user.Id.ToString(), username, accessToken, refreshToken, tokenType: 1);

        return new AuthResult
        {
            Success = true,
            Token = accessToken,
            TwoFactorRequired = false
        };
    }

    await _otpService.SendSmsAsync(user.PhoneNumber);

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
    if (expirationD <= DateTime.UtcNow)
    {
        return new AuthResult
        {
            Success = false,
            Message = "Password expired. Please change your password.",
            TwoFactorRequired = false
        };
    }

    bool isOtpValid =  _otpService.ValidateOtp(otpCode, user.PhoneNumber);
    if (!isOtpValid)
    {
        return new AuthResult
        {
            Success = false,
            Message = "Invalid OTP",
            TwoFactorRequired = true
        };
    }

    var accessToken = _tokenService.GenerateAccessToken(user.Id);
    var refreshToken = _tokenService.GenerateRefreshToken();

    
    
    await SaveOauthTokenAsync(user.Id.ToString(), username, accessToken, refreshToken, tokenType: 1);

    return new AuthResult
    {
        Success = true,
        Token = accessToken,
        TwoFactorRequired = false,
        Message = "AccessToken Generated. Authentication Successful"
    };
}


private async Task SaveOauthTokenAsync(string clientId, string userName, string accessToken, string refreshToken, short tokenType)
{
    var oauthToken = new OauthToken(clientId, userName, accessToken, refreshToken, tokenType);
    await _OauthRepo.AddAsync(oauthToken);
}





public async Task<PassResult> ChangePassword(string username, string exPassword, string newPassword, string confirmPassword)
{
    var user = await _userRepo.GetByUsernameAsync(username);
    if (user == null)
    {
        return new PassResult { Success = false, Message = "Invalid username or password" };
    }

    var confPass = await _userPropertyRepo.GetConfigurationPasswordByUserIdAsync(user.UserProperty.ConfigurationPasswordId);
    if (confPass == null)
    {
        return new PassResult { Success = false, Message = "Password policy settings not found" };
    }

    bool isHashed = user.UserProperty.Password.StartsWith("$2a$") || user.UserProperty.Password.StartsWith("$2b$");

    bool passwordMatches = isHashed
        ? BCrypt.Net.BCrypt.Verify(exPassword, user.UserProperty.Password)
        : user.UserProperty.Password == exPassword;

    if (!passwordMatches)
    {
        return new PassResult { Success = false, Message = "Invalid username or password" };
    }

    if (newPassword != confirmPassword)
    {
        return new PassResult { Success = false, Message = "Passwords do not match" };
    }

    if (newPassword.Length < confPass.MinPassLength || newPassword.Length > confPass.MaxPassLength)
    {
        return new PassResult { Success = false, Message = $"Password must be between {confPass.MinPassLength} and {confPass.MaxPassLength} characters long." };
    }

    if (confPass.MustContainChar && !newPassword.Any(ch => !char.IsLetterOrDigit(ch)))
    {
        return new PassResult { Success = false, Message = "Password must contain at least one special character." };
    }

    if (confPass.MustContainUpperCase && !newPassword.Any(char.IsUpper))
    {
        return new PassResult { Success = false, Message = "Password must contain at least one uppercase letter." };
    }

    if (newPassword.Count(char.IsDigit) < confPass.NumericPassNotEqual)
    {
        return new PassResult { Success = false, Message = $"Password must contain at least {confPass.NumericPassNotEqual} digits." };
    }

    var isReused = await _userPropertyRepo.IsPasswordReusedAsync(user.Id, newPassword);
    if (isReused)
    {
        return new PassResult { Success = false, Message = "You cannot reuse a previous password." };
    }

    string hashedPassword = BCrypt.Net.BCrypt.HashPassword(newPassword);
    user.UserProperty.SetPassword(hashedPassword);

    await _userPropertyRepo.SaveChangesAsync();
    await _userRepo.SaveChangesAsync();
    
    return new PassResult{Success = true, Message = "Password changed successfully"};

    }
}


  

public class PassResult 
{
    public bool Success {get;set;}
    public string? Password {get;set;}
    public string? Message {get;set;}
}

public class AuthResult
{
    public bool Success { get; set; }
    public string? Token { get; set; }
    public string? Message { get; set; }
    public bool TwoFactorRequired { get; set; }
}
