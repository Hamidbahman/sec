using System;


using System.Security.Cryptography;
using System.Text;

namespace Application;
public static class HashingUtility
{
    public static string HashClientSecret(string clientSecret)
    {
        using (SHA256 sha256Hash = SHA256.Create())
        {
            // Compute hash from the client secret
            byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(clientSecret));
            
            // Convert byte array to a string
            StringBuilder builder = new StringBuilder();
            foreach (byte t in bytes)
            {
                builder.Append(t.ToString("x2"));
            }
            return builder.ToString();
        }
    }
}

