using SecureNotes.Core.Security;

namespace SecureNotes.Tests.Unit;

public class AesEncryptionServiceTests
{
    private readonly AesEncryptionService _sut;

    public AesEncryptionServiceTests()
    {
        var key = Convert.ToBase64String(new byte[32]);
        _sut = new AesEncryptionService(key);
    }

    [Fact]
    public void Encrypt_ShouldReturnCipherTextAndIV()
    {
        var (cipherText, iv) = _sut.Encrypt("hello world");

        Assert.NotEmpty(cipherText);
        Assert.NotEmpty(iv);
    }

    [Fact]
    public void Decrypt_ShouldReturnOriginalText()
    {
        var original = "secret note content";
        var (cipherText, iv) = _sut.Encrypt(original);

        var decrypted = _sut.Decrypt(cipherText, iv);

        Assert.Equal(original, decrypted);
    }

    [Fact]
    public void Encrypt_SamePlainText_ShouldReturnDifferentCipherText()
    {
        var (cipher1, _) = _sut.Encrypt("same text");
        var (cipher2, _) = _sut.Encrypt("same text");

        Assert.NotEqual(cipher1, cipher2);
    }

    [Fact]
    public void Decrypt_WithWrongIV_ShouldThrowException()
    {
        var (cipherText, _) = _sut.Encrypt("secret");
        var wrongIv = Convert.ToBase64String(new byte[12]);

        Assert.ThrowsAny<System.Security.Cryptography.CryptographicException>(
            () => _sut.Decrypt(cipherText, wrongIv)
    );
    }
}