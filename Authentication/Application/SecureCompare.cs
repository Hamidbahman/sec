using System;


using System.Security.Cryptography;
using System.Text;
namespace Application;

public static bool SecureCompare(string clientSecret, string storedHashedSecret)
{
    var enteredHash = HashingUtility.HashClientSecret(clientSecret);

    // Securely compare using constant-time comparison
    return CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(enteredHash), Encoding.UTF8.GetBytes(storedHashedSecret));
}
