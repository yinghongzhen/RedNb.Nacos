using Xunit;
using RedNb.Nacos.Core.Config.Filter;

namespace RedNb.Nacos.Tests.Config.Filter;

/// <summary>
/// Tests for <see cref="AesEncryptionConfigFilter"/>.
/// </summary>
public class AesEncryptionConfigFilterTests
{
    [Fact]
    public void FilterName_ShouldReturnAesEncryption()
    {
        // Arrange
        var filter = new AesEncryptionConfigFilter();
        filter.Init(null);

        // Assert
        Assert.Equal("AesEncryptionFilter", filter.FilterName);
    }

    [Fact]
    public void Order_ShouldReturnZero()
    {
        // Arrange
        var filter = new AesEncryptionConfigFilter();
        filter.Init(null);

        // Assert
        Assert.Equal(0, filter.Order);
    }

    [Fact]
    public void Encrypt_ShouldEncryptContent()
    {
        // Arrange
        var filter = new AesEncryptionConfigFilter();
        filter.Init(null);
        var plainText = "Hello, Nacos!";

        // Act
        var encrypted = filter.Encrypt(plainText);

        // Assert
        Assert.NotNull(encrypted);
        Assert.NotEqual(plainText, encrypted);
    }

    [Fact]
    public void Decrypt_ShouldDecryptContent()
    {
        // Arrange
        var filter = new AesEncryptionConfigFilter();
        filter.Init(null);
        var plainText = "Hello, Nacos!";
        var encrypted = filter.Encrypt(plainText);

        // Act
        var decrypted = filter.Decrypt(encrypted);

        // Assert
        Assert.Equal(plainText, decrypted);
    }

    [Fact]
    public void Encrypt_EmptyContent_ShouldEncrypt()
    {
        // Arrange
        var filter = new AesEncryptionConfigFilter();
        filter.Init(null);
        var emptyText = "";

        // Act
        var encrypted = filter.Encrypt(emptyText);

        // Assert
        Assert.NotNull(encrypted);
    }

    [Fact]
    public void Decrypt_EncryptedEmptyContent_ShouldReturnEmpty()
    {
        // Arrange
        var filter = new AesEncryptionConfigFilter();
        filter.Init(null);
        var emptyText = "";
        var encrypted = filter.Encrypt(emptyText);

        // Act
        var decrypted = filter.Decrypt(encrypted);

        // Assert
        Assert.Equal(emptyText, decrypted);
    }

    [Fact]
    public async Task DoFilter_OnGetConfig_ShouldDecryptContent()
    {
        // Arrange
        var filter = new AesEncryptionConfigFilter();
        filter.Init(null);
        var plainText = "Hello, Nacos!";
        var encrypted = AesEncryptionConfigFilter.EncryptedPrefix + filter.Encrypt(plainText);
        
        var request = new ConfigRequest("dataId", "group", null, null);
        var response = new ConfigResponse();
        response.PutParameter(ConfigResponseKeys.Content, encrypted);
        var chain = new MockFilterChain();

        // Act
        await filter.DoFilterAsync(request, response, chain);

        // Assert
        Assert.Equal(plainText, response.GetParameter(ConfigResponseKeys.Content));
        Assert.True(chain.WasCalled);
    }

    [Fact]
    public async Task DoFilter_OnPublishConfig_ShouldEncryptContent()
    {
        // Arrange
        var filter = new AesEncryptionConfigFilter();
        filter.Init(null);
        var plainText = "Hello, Nacos!";
        
        var request = new ConfigRequest("dataId", "group", null, null);
        request.PutParameter(ConfigRequestKeys.Content, plainText);
        var response = new ConfigResponse();
        var chain = new MockFilterChain();

        // Act
        await filter.DoFilterAsync(request, response, chain);

        // Assert
        var content = request.GetParameter(ConfigRequestKeys.Content) as string;
        Assert.StartsWith(AesEncryptionConfigFilter.EncryptedPrefix, content);
        Assert.True(chain.WasCalled);
    }

    [Fact]
    public void RoundTrip_ComplexContent_ShouldPreserve()
    {
        // Arrange
        var filter = new AesEncryptionConfigFilter();
        filter.Init(null);
        var complexContent = @"
        {
            ""database"": {
                ""host"": ""localhost"",
                ""port"": 5432,
                ""password"": ""secret123!@#""
            },
            ""features"": [""auth"", ""logging"", ""caching""]
        }";

        // Act
        var encrypted = filter.Encrypt(complexContent);
        var decrypted = filter.Decrypt(encrypted);

        // Assert
        Assert.Equal(complexContent, decrypted);
    }

    [Fact]
    public void RoundTrip_UnicodeContent_ShouldPreserve()
    {
        // Arrange
        var filter = new AesEncryptionConfigFilter();
        filter.Init(null);
        var unicodeContent = "你好世界 Hello World 🌍 こんにちは";

        // Act
        var encrypted = filter.Encrypt(unicodeContent);
        var decrypted = filter.Decrypt(encrypted);

        // Assert
        Assert.Equal(unicodeContent, decrypted);
    }

    [Fact]
    public void Init_WithCustomKeys_ShouldUseProvidedKeys()
    {
        // Arrange
        var filter = new AesEncryptionConfigFilter();
        var key = AesEncryptionConfigFilter.GenerateNewKey();
        var iv = AesEncryptionConfigFilter.GenerateNewIv();
        var properties = new Dictionary<string, string>
        {
            { AesEncryptionConfigFilter.PropertyKeyAesKey, Convert.ToBase64String(key) },
            { AesEncryptionConfigFilter.PropertyKeyAesIv, Convert.ToBase64String(iv) }
        };

        // Act
        filter.Init(properties);
        var plainText = "Test content";
        var encrypted = filter.Encrypt(plainText);
        var decrypted = filter.Decrypt(encrypted);

        // Assert
        Assert.Equal(plainText, decrypted);
    }

    /// <summary>
    /// Mock filter chain for testing.
    /// </summary>
    private class MockFilterChain : IConfigFilterChain
    {
        public bool WasCalled { get; private set; }

        public Task DoFilterAsync(IConfigRequest request, IConfigResponse response, CancellationToken cancellationToken = default)
        {
            WasCalled = true;
            return Task.CompletedTask;
        }
    }
}
