using System;
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

        public OtpService(IOptions<KavenegarOptions> options)
        {
            _api = new KavenegarApi(options.Value.ApiKey);
            _sender = "2000660110";
        }

        /// <summary>
        /// Generates a 6-digit OTP asynchronously.
        /// </summary>
        public async Task<string> GenerateOtpAsync()
        {
            return await Task.Run(() =>
            {
                Random random = new Random();
                return random.Next(100000, 999999).ToString();
            });
        }

        /// <summary>
        /// Generates an OTP and sends it via SMS using Kavenegar API.
        /// </summary>
public async Task<string> SendSmsAsync(string receptor)
{
    string otpCode = await GenerateOtpAsync(); // Generate OTP

    try
    {
        var result = await _api.Send(_sender, receptor, otpCode);
        Console.WriteLine($"SMS Sent: MessageId={result.Messageid}");
        return otpCode; // Return the OTP after sending
    }
    catch (ApiException ex)
    {
        Console.WriteLine("API Error: " + ex.Message);
    }
    catch (HttpException ex)
    {
        Console.WriteLine("HTTP Error: " + ex.Message);
    }

    return null; // Return null if sending fails
}
    }
}
