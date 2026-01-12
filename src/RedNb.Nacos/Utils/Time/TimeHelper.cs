namespace RedNb.Nacos.Utils.Time;

/// <summary>
/// Time and timestamp helper methods.
/// </summary>
public static class TimeHelper
{
    /// <summary>
    /// Gets current timestamp in milliseconds.
    /// </summary>
    public static long GetCurrentTimeMillis()
    {
        return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }
}
