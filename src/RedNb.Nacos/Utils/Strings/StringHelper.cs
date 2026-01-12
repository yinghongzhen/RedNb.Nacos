namespace RedNb.Nacos.Utils.Strings;

/// <summary>
/// String validation and manipulation helper methods.
/// </summary>
public static class StringHelper
{
    /// <summary>
    /// Checks if a string is null, empty, or whitespace.
    /// </summary>
    public static bool IsBlank(string? value)
    {
        return string.IsNullOrWhiteSpace(value);
    }

    /// <summary>
    /// Checks if a string is not null, empty, or whitespace.
    /// </summary>
    public static bool IsNotBlank(string? value)
    {
        return !string.IsNullOrWhiteSpace(value);
    }

    /// <summary>
    /// Checks if a string is a number.
    /// </summary>
    public static bool IsNumber(string? value)
    {
        return !string.IsNullOrEmpty(value) && long.TryParse(value, out _);
    }
}
