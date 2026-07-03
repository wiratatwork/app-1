using System.Security.Cryptography;
using System.Text;
using App1.Backend.Configuration;
using Microsoft.Extensions.Options;

namespace App1.Backend.Services;

public sealed class TokenCipherService
{
    private const int NonceSize = 12;
    private const int TagSize = 16;
    private readonly byte[] _key;

    public TokenCipherService(IOptions<TokenEncryptionSettings> settings)
    {
        var keyBase64 = settings.Value.Key;
        if (string.IsNullOrWhiteSpace(keyBase64))
        {
            throw new InvalidOperationException("Bff:TokenEncryption:Key is not configured.");
        }

        _key = Convert.FromBase64String(keyBase64);
        if (_key.Length != 32)
        {
            throw new InvalidOperationException("Bff:TokenEncryption:Key must decode to exactly 32 bytes.");
        }
    }

    public string Encrypt(string plaintext)
    {
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
        var ciphertext = new byte[plaintextBytes.Length];
        var tag = new byte[TagSize];

        using var aes = new AesGcm(_key, TagSize);
        aes.Encrypt(nonce, plaintextBytes, ciphertext, tag);

        var payload = new byte[NonceSize + ciphertext.Length + TagSize];
        Buffer.BlockCopy(nonce, 0, payload, 0, NonceSize);
        Buffer.BlockCopy(ciphertext, 0, payload, NonceSize, ciphertext.Length);
        Buffer.BlockCopy(tag, 0, payload, NonceSize + ciphertext.Length, TagSize);
        return Convert.ToBase64String(payload);
    }

    public string Decrypt(string ciphertextBase64)
    {
        var payload = Convert.FromBase64String(ciphertextBase64);
        if (payload.Length < NonceSize + TagSize)
        {
            throw new CryptographicException("Encrypted payload is too short.");
        }

        var nonce = payload.AsSpan(0, NonceSize);
        var tag = payload.AsSpan(payload.Length - TagSize, TagSize);
        var ciphertext = payload.AsSpan(NonceSize, payload.Length - NonceSize - TagSize);
        var plaintext = new byte[ciphertext.Length];

        using var aes = new AesGcm(_key, TagSize);
        aes.Decrypt(nonce, ciphertext, tag, plaintext);
        return Encoding.UTF8.GetString(plaintext);
    }

    public string? EncryptOptional(string? plaintext) =>
        string.IsNullOrEmpty(plaintext) ? null : Encrypt(plaintext);

    public string? DecryptOptional(string? ciphertext) =>
        string.IsNullOrEmpty(ciphertext) ? null : Decrypt(ciphertext);
}
