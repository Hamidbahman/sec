using System;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Authentication.Application
{
    public class KavenegarOtpService
    {
        private readonly Dictionary<long, string> _otpStore = new();
        private readonly Random _random = new();
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<KavenegarOtpService> _logger;

        private const string BaseUrl = "https://api.kavenegar.com/v1";

        public KavenegarOtpService(
            HttpClient httpClient, 
            IConfiguration configuration,
            ILogger<KavenegarOtpService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// Generates a 6-digit OTP for a specific user
        /// </summary>
        /// <param name="userId">User's unique identifier</param>
        /// <returns>Generated OTP</returns>
        public string GenerateOtp(long userId)
        {
            var otp = _random.Next(100000, 999999).ToString();
            _otpStore[userId] = otp;
            return otp;
        }

        /// <summary>
        /// Validates the OTP for a specific user
        /// </summary>
        /// <param name="userId">User's unique identifier</param>
        /// <param name="otpCode">OTP to validate</param>
        /// <returns>True if OTP is valid, false otherwise</returns>
        public bool ValidateOtp(long userId, string otpCode)
        {
            if (!_otpStore.ContainsKey(userId))
                return false;

            bool isValid = _otpStore[userId] == otpCode;
            
            // Clear OTP after validation to prevent reuse
            if (isValid)
                _otpStore.Remove(userId);

            return isValid;
        }

        /// <summary>
        /// Sends OTP via SMS using Kavenegar
        /// </summary>
        /// <param name="phoneNumber">Recipient's phone number</param>
        /// <param name="otp">One-time password</param>
        /// <returns>Task representing the send operation</returns>
        public async Task<bool> SendOtpAsync(string phoneNumber, string otp)
        {
            try
            {
                // Get Kavenegar configuration
                var apiKey = _configuration["Kavenegar:ApiKey"];
                var sender = _configuration["Kavenegar:Sender"] ?? "10006345";
                
                if (string.IsNullOrEmpty(apiKey))
                {
                    _logger.LogError("Kavenegar API key is not configured");
                    return false;
                }

                // Prepare message
                string message = $"Your verification code is: {otp}";

                // Construct API endpoint
                string endpoint = $"{BaseUrl}/{apiKey}/sms/send.json";

                // Prepare request parameters
                var requestUri = $"{endpoint}?receptor={phoneNumber}&message={Uri.EscapeDataString(message)}&sender={sender}";

                // Send the request
                var response = await _httpClient.GetAsync(requestUri);

                // Check response
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation($"OTP sent successfully to {phoneNumber}");
                    return true;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"Failed to send OTP. Status: {response.StatusCode}, Error: {errorContent}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Exception occurred while sending OTP to {phoneNumber}");
                return false;
            }
        }

        /// <summary>
        /// Clears OTP for a specific user
        /// </summary>
        /// <param name="userId">User's unique identifier</param>
        public void ClearOtp(long userId)
        {
            if (_otpStore.ContainsKey(userId))
                _otpStore.Remove(userId);
        }
    }
}

// Configuration in appsettings.json
//{
//  "Kavenegar": {
//    "ApiKey": "your-kavenegar-api-key",
//    "Sender": "10006345" // Optional
//  }
//}

// Startup.cs configuration
//public void ConfigureServices(IServiceCollection services)
//{
//    // Register HttpClient
//    services.AddHttpClient<KavenegarOtpService>();
//    
//    // Add the OTP service
//    services.AddScoped<KavenegarOtpService>();
//}