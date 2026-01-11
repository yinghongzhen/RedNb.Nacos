namespace RedNb.Nacos.Core.Ai.Model.A2a;

/// <summary>
/// Security scheme definition (flexible key-value structure).
/// </summary>
public class SecurityScheme : Dictionary<string, object>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SecurityScheme"/> class.
    /// </summary>
    public SecurityScheme() : base(4)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SecurityScheme"/> class with existing data.
    /// </summary>
    /// <param name="dictionary">The dictionary to copy from.</param>
    public SecurityScheme(IDictionary<string, object> dictionary) : base(dictionary)
    {
    }
}
