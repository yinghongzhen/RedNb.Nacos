namespace RedNb.Nacos.Core;

/// <summary>
/// Property key constants for Nacos client configuration.
/// </summary>
public static class PropertyKeyConst
{
    public const string IsUseCloudNamespaceParsing = "isUseCloudNamespaceParsing";
    public const string IsUseEndpointParsingRule = "isUseEndpointParsingRule";
    public const string Endpoint = "endpoint";
    public const string EndpointQueryParams = "endpointQueryParams";
    public const string EndpointPort = "endpointPort";
    public const string EndpointContextPath = "endpointContextPath";
    public const string EndpointClusterName = "endpointClusterName";
    public const string EndpointRefreshIntervalSeconds = "endpointRefreshIntervalSeconds";
    public const string Namespace = "namespace";
    public const string Username = "username";
    public const string Password = "password";
    public const string AccessKey = "accessKey";
    public const string SecretKey = "secretKey";
    public const string RamRoleName = "ramRoleName";
    public const string ServerAddr = "serverAddr";
    public const string ContextPath = "contextPath";
    public const string ClusterName = "clusterName";
    public const string Encode = "encode";
    public const string ConfigLongPollTimeout = "configLongPollTimeout";
    public const string ConfigRetryTime = "configRetryTime";
    public const string ConfigRequestTimeout = "configRequestTimeout";
    public const string ClientWorkerMaxThreadCount = "clientWorkerMaxThreadCount";
    public const string ClientWorkerThreadCount = "clientWorkerThreadCount";
    public const string MaxRetry = "maxRetry";
    public const string EnableRemoteSyncConfig = "enableRemoteSyncConfig";
    public const string NamingLoadCacheAtStart = "namingLoadCacheAtStart";
    public const string NamingCacheRegistryDir = "namingCacheRegistryDir";
    public const string NamingClientBeatThreadCount = "namingClientBeatThreadCount";
    public const string NamingPollingMaxThreadCount = "namingPollingMaxThreadCount";
    public const string NamingPollingThreadCount = "namingPollingThreadCount";
    public const string NamingRequestDomainRetryCount = "namingRequestDomainMaxRetryCount";
    public const string NamingPushEmptyProtection = "namingPushEmptyProtection";
    public const string NamingAsyncQuerySubscribeService = "namingAsyncQuerySubscribeService";
    public const string RedoDelayTime = "redoDelayTime";
    public const string RedoDelayThreadCount = "redoDelayThreadCount";
    public const string SignatureRegionId = "signatureRegionId";
    public const string LogAllProperties = "logAllProperties";
    public const string IsUseRamInfoParsing = "isUseRamInfoParsing";
    public const string EnableClientMetrics = "enableClientMetrics";
}
