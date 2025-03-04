using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Application;
using Kavenegar;
using Kavenegar.Core.Exceptions;
using Microsoft.Extensions.Options;

namespace Authentication.Application
{
    public class OtpService
    {
        private readonly KavenegarApi _api;
        private readonly string _sender;

        private readonly ConcurrentDictionary<string, OtpInfo> _otpStorage = new();
        
        private readonly ConcurrentDictionary<string, string> _otpToPhoneMap = new();

        public OtpService(IOptions<KavenegarOptions> options)
        {
            _api = new KavenegarApi(options.Value.ApiKey);
            _sender = "2000660110";
        }

        /// <summary>
        /// Generates a 6-digit OTP asynchronously.
        /// </summary>
        private async Task<string> GenerateOtpAsync()
        {
            return await Task.Run(() =>
            {
                Random random = new Random();
                return random.Next(100000, 999999).ToString();
            });
        }

        /// <summary>
        /// Sends an OTP via SMS and stores it with an expiration time.
        /// </summary>
        public async Task<bool> SendSmsAsync(string phoneNumber, string username = null)
        {
            string otpCode = await GenerateOtpAsync();

            try
            {
                var result = await _api.Send(_sender, phoneNumber, otpCode);
                Console.WriteLine($"SMS Sent to {phoneNumber}: MessageId={result.Messageid}");

                _otpStorage[phoneNumber] = new OtpInfo
                {
                    Code = otpCode,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(5),
                    Username = username
                };
                
                _otpToPhoneMap[otpCode] = phoneNumber;

                return true;
            }
            catch (ApiException ex)
            {
                Console.WriteLine("API Error: " + ex.Message);
            }
            catch (HttpException ex)
            {
                Console.WriteLine("HTTP Error: " + ex.Message);
            }

            return false; 
        }

        /// <summary>
        /// Validates the OTP without requiring phone number.
        /// </summary>
        public bool ValidateOtp(string otpCode)
        {
            if (_otpToPhoneMap.TryGetValue(otpCode, out string phoneNumber))
            {
                if (_otpStorage.TryGetValue(phoneNumber, out OtpInfo otpInfo))
                {
                    if (otpInfo.ExpiresAt < DateTime.UtcNow)
                    {
                        CleanupOtp(phoneNumber, otpCode);
                        return false; 
                    }

                    if (otpInfo.Code == otpCode)
                    {
                        CleanupOtp(phoneNumber, otpCode);
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Gets the username associated with an OTP code.
        /// </summary>
        public string GetUsernameByOtp(string otpCode)
        {
            if (_otpToPhoneMap.TryGetValue(otpCode, out string phoneNumber) && 
                _otpStorage.TryGetValue(phoneNumber, out OtpInfo otpInfo))
            {
                return otpInfo.Username;
            }
            return null;
        }
        
        /// <summary>
        /// Original validate method for backward compatibility
        /// </summary>
        public bool ValidateOtp(string phoneNumber, string otpReceived)
        {
            if (_otpStorage.TryGetValue(phoneNumber, out OtpInfo otpInfo))
            {

                if (otpInfo.ExpiresAt < DateTime.UtcNow)
                {
                    CleanupOtp(phoneNumber, otpInfo.Code);
                    return false;
                }

                // Check if OTP matches
                if (otpInfo.Code == otpReceived)
                {
                    CleanupOtp(phoneNumber, otpReceived);
                    return true;
                }
            }

            return false; 
        }
        
        /// <summary>
        /// Clean up OTP entries after use or expiration.
        /// </summary>
        private void CleanupOtp(string phoneNumber, string otpCode)
        {
            _otpStorage.TryRemove(phoneNumber, out _);
            _otpToPhoneMap.TryRemove(otpCode, out _);
        }

        /// <summary>
        /// Represents OTP information.
        /// </summary>
        private class OtpInfo
        {
            public string Code { get; set; }
            public DateTime ExpiresAt { get; set; }
            public string Username { get; set; }
        }
    }
}





