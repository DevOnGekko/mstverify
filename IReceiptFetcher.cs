namespace MstVerify;

/// <summary>
/// Interface for service-specific receipt fetching implementations
/// </summary>
public interface IReceiptFetcher
{
    /// <summary>
    /// Fetches the service version/build number
    /// </summary>
    /// <param name="serviceEndpoint">The service endpoint URL</param>
    /// <returns>The service version string</returns>
    Task<string> FetchServiceVersionAsync(string serviceEndpoint);

    /// <summary>
    /// Downloads the receipt from the service
    /// </summary>
    /// <param name="serviceEndpoint">The service endpoint URL</param>
    /// <returns>The receipt bytes</returns>
    Task<byte[]> DownloadReceiptAsync(string serviceEndpoint);
}
