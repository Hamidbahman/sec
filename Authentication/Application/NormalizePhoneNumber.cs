using System;

namespace Application;

private string NormalizePhoneNumber(string phoneNumber)
{
    // Remove any non-numeric characters (like spaces, dashes, etc.)
    string normalizedPhoneNumber = new string(phoneNumber.Where(char.IsDigit).ToArray());

    // Check if the number starts with the country code (e.g., +98 for Iran)
    if (normalizedPhoneNumber.Length == 10 && normalizedPhoneNumber.StartsWith("9"))
    {
        // Add the country code if it's not present
        normalizedPhoneNumber = "+98" + normalizedPhoneNumber.Substring(1);
    }

    return normalizedPhoneNumber;
}
