using System.Security.Cryptography;
using System.Text;

namespace InvoiceService.Helpers;

public static class PasswordGenerator
{
    private const string Lowercase = "abcdefghijklmnopqrstuvwxyz";
    private const string Uppercase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private const string Digits = "0123456789";
    private const string Special = "!@#$%^&*()-_=+[]{}<>?";

    public static string GenerateTemporaryPassword(int length = 12)
    {
        if (length < 8)
            throw new ArgumentException("Password length must be at least 8 characters.");

        var allChars = Lowercase + Uppercase + Digits + Special;

        var password = new List<char>
        {
            GetRandomChar(Lowercase),
            GetRandomChar(Uppercase),
            GetRandomChar(Digits),
            GetRandomChar(Special)
        };

        for (int i = password.Count; i < length; i++)
        {
            password.Add(GetRandomChar(allChars));
        }

        // Shuffle characters to avoid predictable pattern
        return Shuffle(password);
    }

    private static char GetRandomChar(string chars)
    {
        var index = RandomNumberGenerator.GetInt32(chars.Length);
        return chars[index];
    }

    private static string Shuffle(List<char> chars)
    {
        for (int i = chars.Count - 1; i > 0; i--)
        {
            int j = RandomNumberGenerator.GetInt32(i + 1);
            (chars[i], chars[j]) = (chars[j], chars[i]);
        }

        return new string(chars.ToArray());
    }
}