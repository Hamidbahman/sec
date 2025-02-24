using System;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Authentication.Domain.Repositories;
using Microsoft.Extensions.Configuration;

namespace Authentication.Application
{
    public class OTPService
    {
        private readonly IUserRepository _userRepo;
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private readonly string _apiKey;
        private static ConcurrentDictionary<string, (string otp, DateTime expiry)> otpStore = new();

        public OTPService(IUserRepository userRepo, HttpClient httpClient, IConfiguration configuration)
        {
            _userRepo = userRepo;
            _httpClient = httpClient;
            _baseUrl = configuration["OTPService:BaseUrl"];
            _apiKey = configuration["OTPService:Api_Key"];
        }

        public async Task<string?> GenerateOTPAsync(string phoneNumber, int length = 6)
        {
            var user = await _userRepo.GetUserByPhoneNumber(phoneNumber);
            if (user == null || phoneNumber != user.PhoneNumber)
            {
                return null;
            }

            var otp = new Random().Next(0, (int)Math.Pow(10, length)).ToString($"D{length}");
            otpStore[phoneNumber] = (otp, DateTime.UtcNow.AddMinutes(5));

            bool success = await SendOTPToPhone(phoneNumber, otp);
            return success ? otp : null;
        }

        public bool ValidateOTP(string phoneNumber, string otp)
        {
            if (otpStore.TryGetValue(phoneNumber, out var otpEntry))
            {
                if (otpEntry.otp == otp && otpEntry.expiry > DateTime.UtcNow)
                {
                    otpStore.TryRemove(phoneNumber, out _);
                    return true;
                }
            }
            return false;
        }

        private async Task<bool> SendOTPToPhone(string phoneNumber, string otp)
        {
            var requestBody = new
            {
                apiKey = _apiKey,
                recipient = phoneNumber,
                message = $"Your OTP code is: {otp}"
            };

            var content = new StringContent(JsonSerializer.Serialize(requestBody), System.Text.Encoding.UTF8, "application/json");

            try
            {
                var response = await _httpClient.PostAsync(_baseUrl, content);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send OTP: {ex.Message}");
                return false;
            }
        }
    }
}
