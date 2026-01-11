using System.Security.Cryptography;
using System.Text;

namespace RedNb.Nacos.Core.Config.Filter;

/// <summary>
/// AES encryption config filter for encrypting/decrypting config content.
/// </summary>
public class AesEncryptionConfigFilter : AbstractConfigFilter
{
    private byte[]? _key;
    private byte[]? _iv;

    /// <summary>
    /// Property key for AES encryption key (Base64 encoded, 32 bytes for AES-256).
    /// </summary>
    public const string PropertyKeyAesKey = "nacos.config.filter.aes.key";

    /// <summary>
    /// Property key for AES IV (Base64 encoded, 16 bytes).
    /// </summary>
    public const string PropertyKeyAesIv = "nacos.config.filter.aes.iv";

    /// <summary>
    /// Prefix to identify encrypted content.
    /// </summary>
    public const string EncryptedPrefix = "cipher-aes:";

    public override string FilterName => "AesEncryptionFilter";

    public override int Order => 0; // Execute first

    public override void Init(IDictionary<string, string>? properties)
    {
        base.Init(properties);

        if (properties != null)
        {
            if (properties.TryGetValue(PropertyKeyAesKey, out var keyBase64) && !string.IsNullOrEmpty(keyBase64))
            {
                _key = Convert.FromBase64String(keyBase64);
            }

            if (properties.TryGetValue(PropertyKeyAesIv, out var ivBase64) && !string.IsNullOrEmpty(ivBase64))
            {
                _iv = Convert.FromBase64String(ivBase64);
            }
        }

        // Generate default key/iv if not provided
        _key ??= GenerateDefaultKey();
        _iv ??= GenerateDefaultIv();
    }

    public override async Task DoFilterAsync(IConfigRequest request, IConfigResponse response, IConfigFilterChain filterChain, CancellationToken cancellationToken = default)
    {
        // Before: Encrypt content for publish operations
        var requestContent = request.GetParameter(ConfigRequestKeys.Content) as string;
        if (!string.IsNullOrEmpty(requestContent) && !requestContent.StartsWith(EncryptedPrefix))
        {
            var encrypted = Encrypt(requestContent);
            request.PutParameter(ConfigRequestKeys.Content, EncryptedPrefix + encrypted);
        }

        // Execute next filter
        await filterChain.DoFilterAsync(request, response, cancellationToken);

        // After: Decrypt content for get operations
        var responseContent = response.GetParameter(ConfigResponseKeys.Content) as string;
        if (!string.IsNullOrEmpty(responseContent) && responseContent.StartsWith(EncryptedPrefix))
        {
            var encryptedContent = responseContent[EncryptedPrefix.Length..];
            var decrypted = Decrypt(encryptedContent);
            response.PutParameter(ConfigResponseKeys.Content, decrypted);
        }
    }

    /// <summary>
    /// Encrypts the content using AES.
    /// </summary>
    public string Encrypt(string plainText)
    {
        if (_key == null || _iv == null)
        {
            throw new InvalidOperationException("AES key and IV must be initialized");
        }

        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV = _iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var encryptor = aes.CreateEncryptor();
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var encryptedBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
        return Convert.ToBase64String(encryptedBytes);
    }

    /// <summary>
    /// Decrypts the content using AES.
    /// </summary>
    public string Decrypt(string cipherText)
    {
        if (_key == null || _iv == null)
        {
            throw new InvalidOperationException("AES key and IV must be initialized");
        }

        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV = _iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var decryptor = aes.CreateDecryptor();
        var cipherBytes = Convert.FromBase64String(cipherText);
        var decryptedBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
        return Encoding.UTF8.GetString(decryptedBytes);
    }

    /// <summary>
    /// Generates a new random AES-256 key.
    /// </summary>
    public static byte[] GenerateNewKey()
    {
        var key = new byte[32]; // 256 bits
        RandomNumberGenerator.Fill(key);
        return key;
    }

    /// <summary>
    /// Generates a new random IV.
    /// </summary>
    public static byte[] GenerateNewIv()
    {
        var iv = new byte[16]; // 128 bits
        RandomNumberGenerator.Fill(iv);
        return iv;
    }

    private static byte[] GenerateDefaultKey()
    {
        // Default key for demo purposes - in production, always use a secure random key
        return Encoding.UTF8.GetBytes("NacosDefaultKey!NacosDefaultKey!"); // 32 bytes
    }

    private static byte[] GenerateDefaultIv()
    {
        // Default IV for demo purposes - in production, always use a secure random IV
        return Encoding.UTF8.GetBytes("NacosDefaultIV!!"); // 16 bytes
    }
}
