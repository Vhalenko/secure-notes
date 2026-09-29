using SecureNotes.Core.Entities;

namespace SecureNotes.Core.Interfaces;

public interface IEncryptionService
{
    (string CipherText, string IV) Encrypt(string plainText);
    string Decrypt(string cipherText, string iv);
}