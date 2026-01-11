using System.Text.Json.Serialization;

namespace RedNb.Nacos.Core.Naming;

/// <summary>
/// List view for paginated results.
/// </summary>
/// <typeparam name="T">The type of items in the list.</typeparam>
public class ListView<T>
{
    /// <summary>
    /// Total count of items.
    /// </summary>
    [JsonPropertyName("count")]
    public int Count { get; set; }

    /// <summary>
    /// List of items.
    /// </summary>
    [JsonPropertyName("data")]
    public List<T> Data { get; set; } = new();

    public ListView()
    {
    }

    public ListView(int count, List<T> data)
    {
        Count = count;
        Data = data;
    }
}
