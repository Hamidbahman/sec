using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using BCrypt.Net;
using Application;
using Authentication.Domain.Entities;
using Authentication.Domain.Repositories;
using Domain.Repositories;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using StackExchange.Redis;

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
        private readonly IOAuthTokenRepository _OauthRepo;
        private readonly IDatabase _redis;
        private static readonly ConcurrentDictionary<string, string> _authCodes = new();

        public OAuthService(
            IOAuthTokenRepository oauthRepo,
            PuzzleCaptchaService puzzleCaptchaService,
            IUserPropertyRepository userPropertyRepository,
            TokenService tokenService,
            CheckboxCaptchaService checkboxCaptchaService,
            OtpService otpService,
            IConnectionMultiplexer redis,
            IApplicationRepository applicationRepository,
            IUserRepository userRepository)
        {
            _OauthRepo = oauthRepo;
            _puzzleService = puzzleCaptchaService;
            _userPropertyRepo = userPropertyRepository;
            _tokenService = tokenService;
            _checkBox = checkboxCaptchaService;
            _applicationRepository = applicationRepository;
            _userRepo = userRepository;
            _otpService = otpService;
            _redis = redis.GetDatabase();
        }

        public async Task<string?> GenerateAuthorizationCodeAsync(string clientId, string clientSecret, string? userCaptchaToken = null)
        {
            if (await IsRateLimited(clientId))
            {
                return "Please wait before generating another code.";
            }

            var application = await _applicationRepository.GetApplicationByClientIdAsync(clientId);
            if (application == null || !VerifyHashedSecret(clientSecret, application.ClientSecret))
                return null;

            var configLock = await _applicationRepository.GetConfigurationLockAsync(clientId);
            await _applicationRepository.SaveChangesAsync();

            if (configLock.CaptchaNeeded)
            {
                if (string.IsNullOrEmpty(userCaptchaToken))
                    return _checkBox.GenerateCaptchaToken();

                if (!_checkBox.ValidateCaptchaToken(userCaptchaToken))
                    return "InvalidCaptcha";
            }

            await _redis.StringSetAsync($"rate_limit:{clientId}", DateTime.UtcNow.ToString(), TimeSpan.FromSeconds(30));

            string authCode = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
            string encryptedAuthCode = EncryptAuthCode(authCode);
            _authCodes.TryAdd(authCode, clientId);

            await _redis.StringSetAsync($"auth_code:{encryptedAuthCode}", clientId, TimeSpan.FromMinutes(10));

            return authCode;
        }

        private async Task<bool> IsRateLimited(string clientId)
        {
            var lastRequest = await _redis.StringGetAsync($"rate_limit:{clientId}");
            if (!lastRequest.IsNullOrEmpty)
            {
                var lastRequestTime = DateTime.Parse(lastRequest);
                return (DateTime.UtcNow - lastRequestTime).TotalSeconds < 30;
            }
            return false;
        }

        public async Task<bool> IsAuthorizationCodeExpiredAsync(string authCode)
        {
            string encryptedAuthCode = EncryptAuthCode(authCode);
            var value = await _redis.StringGetAsync($"auth_code:{encryptedAuthCode}");
            return value.IsNullOrEmpty;
        }

        public async Task ClearExpiredAuthorizationCodes()
        {
            var server = _redis.Multiplexer.GetServer(_redis.Multiplexer.GetEndPoints().First());
            foreach (var key in server.Keys(pattern: "auth_code:*"))
            {
                if ((await _redis.StringGetAsync(key)).IsNullOrEmpty)
                {
                    await _redis.KeyDeleteAsync(key);
                }
            }
        }

        private static bool VerifyHashedSecret(string inputSecret, string storedHash)
        {
            return HashSecret(inputSecret) == storedHash;
        }

private static string HashSecret(string secret)
{
    byte[] salt = new byte[16];  
    using (var rng = RandomNumberGenerator.Create())
    {
        rng.GetBytes(salt);  
    }

    byte[] hash = KeyDerivation.Pbkdf2(
        password: secret,
        salt: salt,
        prf: KeyDerivationPrf.HMACSHA256,
        iterationCount: 10000,
        numBytesRequested: 32
    );

    byte[] hashBytes = new byte[salt.Length + hash.Length];
    Buffer.BlockCopy(salt, 0, hashBytes, 0, salt.Length);
    Buffer.BlockCopy(hash, 0, hashBytes, salt.Length, hash.Length);

    return Convert.ToBase64String(hashBytes);
}

        private static string EncryptAuthCode(string authCode)
        {
            using (var sha256 = SHA256.Create())
            {
                byte[] hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(authCode));
                return Convert.ToBase64String(hash);
            }
        }





    public async Task<AuthResult> LoginAsync(string username, string password, string authenticationCode)
    {
        var user = await _userRepo.GetByUsernameAsync(username);
        if(user == null)
        {
            return new AuthResult {
                Success = false, 
                Message = "Invalid username or password",
                TwoFactorRequired = false};
        }
        if(user.LoginAttempt >= 5)
        {
            return new AuthResult {
                Success = false,
                Message = "Too many failed attempts",
                TwoFactorRequired = false
            };
        }
        
        if (!BCrypt.Net.BCrypt.Verify(password , user.UserProperty.Password))
        {
            user.IncrementLoginAttempt();
            return new AuthResult
            {
                Success = false,
                Message = "Invalid username or password",
                TwoFactorRequired = false
            };
        }
        
        if (!_authCodes.ContainsKey(authenticationCode))
        {
            throw new AuthenticationException("Invalid authentication code");
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
            
            user.ResetLoginAttempt();
            
            await _userRepo.SaveChangesAsync();
            await _userPropertyRepo.SaveChangesAsync();
            
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

public async Task<AuthResult> VerifyOtpAsync(string otpCode)
{
    if (!_otpService.ValidateOtp(otpCode))
    {
        return new AuthResult
        {
            Success = false,
            Message = "Invalid or expired OTP",
            TwoFactorRequired = false
        };
    }

    string phoneNumber = _otpService.GetPhoneNumberByOtp(otpCode);
    
    if (string.IsNullOrEmpty(phoneNumber))
    {
        return new AuthResult
        {
            Success = false,
            Message = "Unable to retrieve phone number",
            TwoFactorRequired = false
        };
    }

    var user = await _userRepo.GetUserByPhoneNumber(phoneNumber);
    
    if (user == null)
    {
        return new AuthResult
        {
            Success = false,
            Message = "User not found",
            TwoFactorRequired = false
        };
    }

    var confPass = await _userPropertyRepo.GetConfigurationPasswordByUserIdAsync(user.Id);

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

        

        var accessToken = _tokenService.GenerateAccessToken(user.Id);
        var refreshToken = _tokenService.GenerateRefreshToken();

        user.ResetLoginAttempt();
        await _userRepo.SaveChangesAsync();
        await _userPropertyRepo.SaveChangesAsync();
        
        await SaveOauthTokenAsync(user.Id.ToString(), user.Username, accessToken, refreshToken, tokenType: 1);

        return new AuthResult
        {
            Success = true,
            Token = accessToken,
            TwoFactorRequired = false,
            Message = "AccessToken Generated. Authentication Successful",
            User = UserDetails.FromUser(user)
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
        return new PassResult { Success = false, Message = "Invalid username" };
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
        return new PassResult { Success = false, Message = "Invalid password" };
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

    if (newPassword == exPassword)
    {
        return new PassResult { Success = false, Message = "You cannot reuse your current password." };
    }

    string hashedPassword = BCrypt.Net.BCrypt.HashPassword(newPassword);
    user.UserProperty.UpdatePassword(hashedPassword);

    await _userPropertyRepo.SaveChangesAsync();
    await _userRepo.SaveChangesAsync();

    return new PassResult { Success = true, Message = "Password changed successfully" };
}


public class PassResult 
{
    public bool Success { get; set; }
    public string? Password { get; set; }
    public string? Message { get; set; }
}

public class AuthResult
{
    public bool Success { get; set; }
    public string? Token { get; set; }
    public string? Message { get; set; }
    public bool TwoFactorRequired { get; set; }
    
    public UserDetails? User { get; set; }
}

public class UserDetails
{
    public string? Username { get; set; }
    public string? Uuid { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Picture { get; set; }
    public string? PictureType { get; set; }
    
    public static UserDetails FromUser(User user)
    {


        if (user == null) return null;
        
        return new UserDetails
        {
            Username = user.Username,
            Uuid = user.Uuid,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            Picture = user.Picture,
            PictureType = user.PictureType
        };
    }
}
    }
}
    