using System.Security.Cryptography;

namespace M365Manager.Helpers;

public static class PasswordGenerator
{
    private const string Uppercase = "ABCDEFGHJKLMNPQRSTUVWXYZ";
    private const string Lowercase = "abcdefghjkmnpqrstuvwxyz";
    private const string Digits = "23456789";
    private const string Special = "!@#$%&*?";

    /// <summary>
    /// Generates a random password that meets Microsoft 365 complexity requirements.
    /// Avoids ambiguous characters (0/O, 1/l/I).
    /// </summary>
    public static string Generate(int length = 12)
    {
        if (length < 8) length = 8;

        var allChars = Uppercase + Lowercase + Digits + Special;
        var password = new char[length];

        // Guarantee at least one of each category.
        password[0] = Uppercase[RandomNumberGenerator.GetInt32(Uppercase.Length)];
        password[1] = Lowercase[RandomNumberGenerator.GetInt32(Lowercase.Length)];
        password[2] = Digits[RandomNumberGenerator.GetInt32(Digits.Length)];
        password[3] = Special[RandomNumberGenerator.GetInt32(Special.Length)];

        for (int i = 4; i < length; i++)
        {
            password[i] = allChars[RandomNumberGenerator.GetInt32(allChars.Length)];
        }

        // Shuffle to avoid predictable pattern.
        for (int i = password.Length - 1; i > 0; i--)
        {
            int j = RandomNumberGenerator.GetInt32(i + 1);
            (password[i], password[j]) = (password[j], password[i]);
        }

        return new string(password);
    }
}
