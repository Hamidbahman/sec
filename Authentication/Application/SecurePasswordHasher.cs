using System;

namespace Application;

public class PasswordHasherService
{
    private readonly PasswordHasher<Application> _passwordHasher;

    public PasswordHasherService()
    {
        _passwordHasher = new PasswordHasher<Application>();
    }

    // Hash the client secret before storing it
    public string HashClientSecret(string secret)
    {
        return _passwordHasher.HashPassword(null, secret);
    }

    // Verify the stored hashed secret against the input
    public bool VerifyHashedPassword(string hashedSecret, string inputSecret)
    {
        return _passwordHasher.VerifyHashedPassword(null, hashedSecret, inputSecret) == PasswordVerificationResult.Success;
    }
}
