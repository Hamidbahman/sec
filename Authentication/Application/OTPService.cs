
namespace Authentication.Application;
public class OtpService
{
    private readonly Dictionary<int, string> _otpStore = new();
    private readonly Random _random = new();

    public string GenerateOtp(int userId)
    {
        var otp = _random.Next(100000, 999999).ToString();
        _otpStore[userId] = otp;
        return otp;
    }

    public bool ValidateOtp(int userId, string otpCode)
    {
        return _otpStore.ContainsKey(userId) && _otpStore[userId] == otpCode;
    }

    public void SendOtp(string phoneNumber, string otp)
    {
        Console.WriteLine($"Sending OTP {otp} to {phoneNumber}"); // Replace with SMS service
    }
}