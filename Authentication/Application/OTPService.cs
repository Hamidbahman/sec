using System;
using System.Collections.Concurrent;
using System.Net.Mail;
using System.Security.Cryptography;
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
        /// Generates a secure 6-digit OTP asynchronously.
        /// </summary>
        private async Task<string> GenerateOtpAsync()
        {
            return await Task.Run(() =>
            {
                using var rng = new RNGCryptoServiceProvider();
                var bytes = new byte[4];
                rng.GetBytes(bytes);
                int value = BitConverter.ToInt32(bytes, 0) & 0x7FFFFFFF;
                return (value % 900000 + 100000).ToString(); 
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

                var otpInfo = new OtpInfo
                {
                    Code = otpCode,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(5)
                };

                _otpStorage[phoneNumber] = otpInfo;
                _otpToPhoneMap[otpCode] = phoneNumber;
                
                return true;
            }
            catch (ApiException ex)
            {
                Console.WriteLine("API Error: " + ex.Message);
                return false;
            }
            catch (SmtpException ex)
            {
                Console.WriteLine("HTTP Error: " + ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Validates the OTP without requiring a phone number.
        /// </summary>
        public bool ValidateOtp(string otpCode)
        {
            if (_otpToPhoneMap.TryRemove(otpCode, out string phoneNumber) && _otpStorage.TryRemove(phoneNumber, out OtpInfo otpInfo))
            {
                if (otpInfo.ExpiresAt < DateTime.UtcNow)
                {
                    return false;
                }
                return otpInfo.Code == otpCode;
            }
            return false;
        }

        /// <summary>
        /// Gets the phone number associated with an OTP code.
        /// </summary>
        public string GetPhoneNumberByOtp(string otpCode)
        {
            if (_otpToPhoneMap.TryGetValue(otpCode, out string phoneNumber) && _otpStorage.TryGetValue(phoneNumber, out OtpInfo otpInfo))
            {
                return phoneNumber;
            }
            return null;
        }

        /// <summary>
        /// Original validate method for backward compatibility.
        /// </summary>
        public bool ValidateOtp(string phoneNumber, string otpReceived)
        {
            if (_otpStorage.TryRemove(phoneNumber, out OtpInfo otpInfo))
            {
                if (otpInfo.ExpiresAt < DateTime.UtcNow)
                {
                    return false;
                }
                return otpInfo.Code == otpReceived;
            }
            return false;
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