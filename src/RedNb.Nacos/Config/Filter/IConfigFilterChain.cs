namespace RedNb.Nacos.Core.Config.Filter;

/// <summary>
/// Config filter chain interface.
/// </summary>
public interface IConfigFilterChain
{
    /// <summary>
    /// Executes the filter chain.
    /// </summary>
    /// <param name="request">Config request</param>
    /// <param name="response">Config response</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task DoFilterAsync(IConfigRequest request, IConfigResponse response, CancellationToken cancellationToken = default);
}
