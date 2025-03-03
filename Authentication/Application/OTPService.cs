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

        // Store OTPs by phone number (thread-safe)
        private readonly ConcurrentDictionary<string, OtpInfo> _otpStorage = new();

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
        public async Task<bool> SendSmsAsync(string phoneNumber)
        {
            string otpCode = await GenerateOtpAsync();

            try
            {
                var result = await _api.Send(_sender, phoneNumber, otpCode);
                Console.WriteLine($"SMS Sent to {phoneNumber}: MessageId={result.Messageid}");

                // Store OTP with expiration time
                _otpStorage[phoneNumber] = new OtpInfo
                {
                    Code = otpCode,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(5) // OTP expires in 5 minutes
                };

                return true; // Successfully sent OTP
            }
            catch (ApiException ex)
            {
                Console.WriteLine("API Error: " + ex.Message);
            }
            catch (HttpException ex)
            {
                Console.WriteLine("HTTP Error: " + ex.Message);
            }

            return false; // Failed to send OTP
        }

        /// <summary>
        /// Validates the OTP for a given phone number.
        /// </summary>
        public bool ValidateOtp(string phoneNumber, string otpReceived)
        {
            if (_otpStorage.TryGetValue(phoneNumber, out OtpInfo otpInfo))
            {
                // Check if OTP is expired
                if (otpInfo.ExpiresAt < DateTime.UtcNow)
                {
                    _otpStorage.TryRemove(phoneNumber, out _); // Remove expired OTP
                    return false;
                }

                // Check if OTP matches
                if (otpInfo.Code == otpReceived)
                {
                    _otpStorage.TryRemove(phoneNumber, out _); // Remove OTP after successful use
                    return true;
                }
            }
            return false; // OTP not found or incorrect
        }

        /// <summary>
        /// Represents OTP information.
        /// </summary>
        private class OtpInfo
        {
            public string Code { get; set; }
            public DateTime ExpiresAt { get; set; }
        }
    }
}
