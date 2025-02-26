1. Generate RefreshToken
2. Command OAuthToken into DB

```csharp
public class OAuthService
{
    // Existing code...
    
    private static readonly ConcurrentDictionary<string, (string ClientId, string Username)> _authCodes = new();

    // Existing constructor...

    public async Task<string?> GenerateAuthorizationCodeAsync(string clientId, string clientSecret, string? userCaptchaToken = null)
    {
        var failedAttempt = 0;
        var application = await _applicationRepository.GetApplicationByClientIdAsync(clientId);
        if (application == null || clientSecret != application.ClientSecret)
            return null;

        var configLock = await _applicationRepository.GetConfigurationLockAsync(clientId);
        failedAttempt++;
        await _applicationRepository.SaveChangesAsync();
        if (failedAttempt > 3)
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
        _authCodes.TryAdd(authCode, (clientId, "username-placeholder"));
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
        if (logPol != null && user.LoginAttempt > 5)
        {
            logPol.SetLockType(Domain.Enums.LockTypes.TemporaryLock);
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

    public async Task<AuthResult> VerifyOtpAsync(string username, string otpCode, string authenticationCode)
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

        if (user.IpRange == "")
        {
            return new AuthResult
            {
                Success = false,
                Message = "Invalid OTP",
                TwoFactorRequired = true
            };
        }

        // Validate OTP
        var token = _tokenService.GenerateAccessToken(user.Id);

        // Retrieve clientId from _authCodes
        if (!_authCodes.TryGetValue(authenticationCode, out var authCodeData))
        {
            return new AuthResult
            {
                Success = false,
                Message = "Invalid authentication code",
                TwoFactorRequired = false
            };
        }
        var clientId = authCodeData.ClientId;

        // Save the token information to the OauthToken table
        await SaveOauthTokenAsync(clientId, user.UserName, token.AccessToken, token.RefreshToken, 1);

        return new AuthResult
        {
            Success = true,
            Token = token,
            TwoFactorRequired = false,
            Message = "AccessToken Generated Authentication Successful"
        };
    }

    private async Task SaveOauthTokenAsync(string clientId, string userName, string accessToken, string refreshToken, short tokenType)
    {
        var oauthToken = new OauthToken(clientId, userName, accessToken, refreshToken, tokenType);
        await _OauthRepo.AddAsync(oauthToken);
    }
}


```