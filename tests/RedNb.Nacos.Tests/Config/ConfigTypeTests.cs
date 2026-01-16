using FluentAssertions;
using RedNb.Nacos.Core.Config;
using Xunit;

namespace RedNb.Nacos.Tests.Config;

public class ConfigTypeTests
{
    [Theory]
    [InlineData("properties", ConfigType.Properties)]
    [InlineData("xml", ConfigType.Xml)]
    [InlineData("json", ConfigType.Json)]
    [InlineData("txt", ConfigType.Text)]
    [InlineData("html", ConfigType.Html)]
    [InlineData("htm", ConfigType.Html)]
    [InlineData("yaml", ConfigType.Yaml)]
    [InlineData("yml", ConfigType.Yaml)]
    [InlineData("toml", ConfigType.Toml)]
    public void GetTypeByExtension_ValidExtensions_ShouldReturnCorrectType(string extension, string expectedType)
    {
        // Act
        var result = ConfigType.GetTypeByExtension(extension);

        // Assert
        result.Should().Be(expectedType);
    }

    [Theory]
    [InlineData(".json", ConfigType.Json)]
    [InlineData(".YAML", ConfigType.Yaml)]
    [InlineData(".XML", ConfigType.Xml)]
    public void GetTypeByExtension_WithDotAndCasing_ShouldReturnCorrectType(string extension, string expectedType)
    {
        // Act
        var result = ConfigType.GetTypeByExtension(extension);

        // Assert
        result.Should().Be(expectedType);
    }

    [Theory]
    [InlineData("unknown")]
    [InlineData("")]
    [InlineData("abc")]
    public void GetTypeByExtension_UnknownExtension_ShouldReturnText(string extension)
    {
        // Act
        var result = ConfigType.GetTypeByExtension(extension);

        // Assert
        result.Should().Be(ConfigType.Text);
    }

    [Theory]
    [InlineData(ConfigType.Properties, true)]
    [InlineData(ConfigType.Xml, true)]
    [InlineData(ConfigType.Json, true)]
    [InlineData(ConfigType.Text, true)]
    [InlineData(ConfigType.Html, true)]
    [InlineData(ConfigType.Yaml, true)]
    [InlineData(ConfigType.Toml, true)]
    [InlineData("invalid", false)]
    [InlineData("", false)]
    [InlineData("xyz", false)]
    public void IsValidType_ShouldReturnExpectedResult(string type, bool expected)
    {
        // Act
        var result = ConfigType.IsValidType(type);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void ConfigType_Constants_ShouldHaveCorrectValues()
    {
        // Assert
        ConfigType.Properties.Should().Be("properties");
        ConfigType.Xml.Should().Be("xml");
        ConfigType.Json.Should().Be("json");
        ConfigType.Text.Should().Be("text");
        ConfigType.Html.Should().Be("html");
        ConfigType.Yaml.Should().Be("yaml");
        ConfigType.Toml.Should().Be("toml");
        ConfigType.Default.Should().Be(ConfigType.Text);
    }
}
