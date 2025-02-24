using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Authentication.Application
{
    public class OTPService
    {
        private readonly string _username; // Your service username
        private readonly string _password; // Your service password
        private readonly string _fromNumber; // The sender's phone number (fromNum)
        private readonly Send _smsService;

        // Constructor initializing the necessary fields
        public OTPService(string username, string password, string fromNumber)
        {
            _username = username;
            _password = password;
            _fromNumber = fromNumber;
            _smsService = new Send(_username, _password); // Assuming Send class from your code
        }

        // Generate a random 6-digit OTP
        private string GenerateOtp()
        {
            var random = new Random();
            var otp = random.Next(100000, 999999).ToString(); // Generate 6-digit OTP
            return otp;
        }

        // Send OTP via SMS to a given phone number
        public async Task<bool> SendOtpAsync(string toPhoneNumber)
        {
            string otp = GenerateOtp();
            string message = $"Your OTP code is: {otp}"; // OTP message content

            try
            {
                // Send SMS using SendSMS method from the Send class
                string[] toNumbers = new string[] { toPhoneNumber };
                string[] content = new string[] { message };
                string[] type = new string[] { "text" }; // Assuming "text" is the type of message

                var response = await Task.Run(() => _smsService.SendSMS(_fromNumber, toNumbers, message, "text", _username, _password));

                if (response != null && response.Any())
                {
                    // OTP is sent successfully
                    // Save the OTP and associate it with the phone number for later validation
                    SaveOtpToDatabase(toPhoneNumber, otp); // Implement a method to save OTP to your database
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending OTP: {ex.Message}");
            }
            return false;
        }

        // Simulate saving OTP to database (implement actual DB logic)
        private void SaveOtpToDatabase(string phoneNumber, string otp)
        {
            // You should implement a database call here to save the OTP
            // Example: Save the OTP along with the phone number and timestamp for validation

            // In a real scenario, you can store this in a secure DB with expiration times
            Console.WriteLine($"Saving OTP for {phoneNumber}: {otp}");
        }

        // Validate OTP (for example, checking if the provided OTP matches the one sent)
        public bool ValidateOtp(string phoneNumber, string providedOtp)
        {
            // Retrieve OTP from database (implement DB retrieval logic)
            var storedOtp = GetOtpFromDatabase(phoneNumber); // Simulate DB retrieval

            if (storedOtp == providedOtp)
            {
                return true; // OTP is valid
            }
            return false; // OTP is invalid
        }

        // Simulate retrieving OTP from database (implement actual DB logic)
        private string GetOtpFromDatabase(string phoneNumber)
        {
            // Example: Retrieve OTP from DB for the phone number
            // In real-world scenarios, you would check for expiration time as well
            return "123456"; // This should come from the actual DB
        }
    }
}
