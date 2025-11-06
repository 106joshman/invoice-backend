using System.Security.Cryptography;
using System.Text;

namespace InvoiceService.Helpers;

public class EncryptionHelper(IConfiguration configuration)
{
    private readonly string _key = configuration["Encryption:Key"]
        ?? throw new InvalidOperationException("Encryption Key not found in configuration.");
    private readonly string _iv = configuration["Encryption:IV"]
        ?? throw new InvalidOperationException("Encryption IV not found in configuration.");



    public string Encrypt(string plainText)
    {
        if (string.IsNullOrWhiteSpace(plainText))
            return string.Empty;

        var keyBytes = Convert.FromBase64String(_key);
        var ivBytes = Convert.FromBase64String(_iv);

        using var aes = Aes.Create();
        aes.Key = keyBytes;
        aes.IV = ivBytes;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
        using var ms = new MemoryStream();
        using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
        using (var sw = new StreamWriter(cs))
            sw.Write(plainText);

        return Convert.ToBase64String(ms.ToArray());
    }

    public string Decrypt(string cipherText)
    {
        if (string.IsNullOrWhiteSpace(cipherText))
            return string.Empty;

        var keyBytes = Convert.FromBase64String(_key);
        var ivBytes = Convert.FromBase64String(_iv);

        using var aes = Aes.Create();
        aes.Key = keyBytes;
        aes.IV = ivBytes;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        var buffer = Convert.FromBase64String(cipherText);
        using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
        using var ms = new MemoryStream(buffer);
        using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
        using var sr = new StreamReader(cs);
        return sr.ReadToEnd();
    }
}
