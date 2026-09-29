using System.Security.Cryptography;
using System.Text;
using SecureNotes.Core.Interfaces;

namespace SecureNotes.Core.Security;

public class AesEncryptionService : IEncryptionService
{
    private readonly byte[] _key;

    public AesEncryptionService(string base64Key)
    {
        _key = Convert.FromBase64String(base64Key);

        if (_key.Length != 32)
            throw new ArgumentException("Key must be 256 bits (32 bytes).");
    }

    public (string CipherText, string IV) Encrypt(string plainText)
    {
        var iv = RandomNumberGenerator.GetBytes(12);

        using var aes = new AesGcm(_key, AesGcm.TagByteSizes.MaxSize);

        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var cipherBytes = new byte[plainBytes.Length];
        var tag = new byte[AesGcm.TagByteSizes.MaxSize];

        aes.Encrypt(iv, plainBytes, cipherBytes, tag);

        var combined = new byte[cipherBytes.Length + tag.Length];
        cipherBytes.CopyTo(combined, 0);
        tag.CopyTo(combined, cipherBytes.Length);

        return (Convert.ToBase64String(combined), Convert.ToBase64String(iv));
    }

    public string Decrypt(string cipherText, string iv)
    {
        var ivBytes = Convert.FromBase64String(iv);
        var combined = Convert.FromBase64String(cipherText);

        var tagLength = AesGcm.TagByteSizes.MaxSize;
        var cipherBytes = combined[..^tagLength];
        var tag = combined[^tagLength..];

        using var aes = new AesGcm(_key, AesGcm.TagByteSizes.MaxSize);

        var plainBytes = new byte[cipherBytes.Length];
        aes.Decrypt(ivBytes, cipherBytes, tag, plainBytes);

        return Encoding.UTF8.GetString(plainBytes);
    }
}