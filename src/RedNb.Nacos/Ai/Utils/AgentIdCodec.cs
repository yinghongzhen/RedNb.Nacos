using System.Text;

namespace RedNb.Nacos.Core.Ai.Utils;

/// <summary>
/// Agent ID codec interface for encoding/decoding agent names.
/// </summary>
/// <remarks>
/// Agent and AgentCard allow user custom agent name without limit,
/// but when storing in Nacos, it should match some word limits.
/// We need to encode and decode agent name as the identity to do storage.
/// </remarks>
public interface IAgentIdCodec
{
    /// <summary>
    /// Encodes agent name to identity.
    /// </summary>
    /// <param name="agentName">Agent name.</param>
    /// <returns>Identity encoded from agent name.</returns>
    string Encode(string agentName);

    /// <summary>
    /// Encodes agent name to identity for search (without prefix/suffix).
    /// </summary>
    /// <param name="agentName">Agent name.</param>
    /// <returns>Identity encoded from agent name for blur search.</returns>
    string EncodeForSearch(string agentName);

    /// <summary>
    /// Decodes agent id to agent name.
    /// </summary>
    /// <param name="agentId">Agent identity.</param>
    /// <returns>Agent name.</returns>
    string Decode(string agentId);
}

/// <summary>
/// ASCII-based implementation of Agent ID codec.
/// </summary>
/// <remarks>
/// Encoding rules:
/// - Prefix "____:" identifies encoded names
/// - Letters and valid characters (-, ., :) remain unchanged
/// - Other characters are encoded as _XXX (3-digit ASCII code)
/// </remarks>
public class AsciiAgentIdCodec : IAgentIdCodec
{
    /// <summary>
    /// Prefix for encoded agent names.
    /// </summary>
    public const string EncodedPrefix = "____:";

    /// <summary>
    /// Valid characters that don't need encoding.
    /// </summary>
    private static readonly HashSet<char> ValidChars = new() { '-', '.', ':' };

    /// <summary>
    /// Singleton instance.
    /// </summary>
    public static readonly AsciiAgentIdCodec Instance = new();

    /// <inheritdoc />
    public string Encode(string agentName)
    {
        if (string.IsNullOrEmpty(agentName))
        {
            return agentName;
        }

        // Check if encoding is needed
        if (!NeedsEncoding(agentName))
        {
            return agentName;
        }

        return EncodedPrefix + EncodeForSearch(agentName);
    }

    /// <inheritdoc />
    public string EncodeForSearch(string agentName)
    {
        if (string.IsNullOrEmpty(agentName))
        {
            return agentName;
        }

        var sb = new StringBuilder(agentName.Length * 2);

        foreach (var c in agentName)
        {
            if (char.IsLetterOrDigit(c) || ValidChars.Contains(c))
            {
                sb.Append(c);
            }
            else
            {
                // Encode as _XXX (3-digit ASCII code)
                sb.Append('_');
                sb.Append(((int)c).ToString("D3"));
            }
        }

        return sb.ToString();
    }

    /// <inheritdoc />
    public string Decode(string agentId)
    {
        if (string.IsNullOrEmpty(agentId))
        {
            return agentId;
        }

        // Check if this is an encoded name
        if (!agentId.StartsWith(EncodedPrefix))
        {
            return agentId;
        }

        var encoded = agentId.Substring(EncodedPrefix.Length);
        return DecodeInternal(encoded);
    }

    /// <summary>
    /// Checks if agent name needs encoding.
    /// </summary>
    private static bool NeedsEncoding(string agentName)
    {
        foreach (var c in agentName)
        {
            if (!char.IsLetterOrDigit(c) && !ValidChars.Contains(c))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Decodes the encoded part (without prefix).
    /// </summary>
    private static string DecodeInternal(string encoded)
    {
        var sb = new StringBuilder(encoded.Length);
        var i = 0;

        while (i < encoded.Length)
        {
            var c = encoded[i];

            if (c == '_' && i + 3 < encoded.Length)
            {
                // Try to decode _XXX
                var code = encoded.Substring(i + 1, 3);
                if (int.TryParse(code, out var ascii) && ascii >= 0 && ascii <= 127)
                {
                    sb.Append((char)ascii);
                    i += 4;
                    continue;
                }
            }

            sb.Append(c);
            i++;
        }

        return sb.ToString();
    }
}

/// <summary>
/// Static holder for the default Agent ID codec.
/// </summary>
public static class AgentIdCodecHolder
{
    /// <summary>
    /// Gets the default codec instance.
    /// </summary>
    public static IAgentIdCodec Default { get; } = AsciiAgentIdCodec.Instance;

    /// <summary>
    /// Encodes agent name using the default codec.
    /// </summary>
    public static string Encode(string agentName) => Default.Encode(agentName);

    /// <summary>
    /// Encodes agent name for search using the default codec.
    /// </summary>
    public static string EncodeForSearch(string agentName) => Default.EncodeForSearch(agentName);

    /// <summary>
    /// Decodes agent id using the default codec.
    /// </summary>
    public static string Decode(string agentId) => Default.Decode(agentId);
}
