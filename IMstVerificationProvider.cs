using System.Security.Cryptography.X509Certificates;

namespace MstVerify;

/// <summary>
/// Interface for MST verification support operations
/// </summary>
public interface IMstVerificationProvider
{
    /// <summary>
    /// Downloads the root certificate from the MST endpoint
    /// </summary>
    /// <param name="mstEndpoint">The MST service endpoint URL</param>
    /// <returns>The root certificate</returns>
    Task<X509Certificate2> DownloadRootCertificateAsync(string mstEndpoint);

    /// <summary>
    /// Verifies a receipt against the root certificate
    /// </summary>
    /// <param name="receiptBytes">The receipt bytes to verify</param>
    /// <param name="rootCertificate">The root certificate to use for verification</param>
    /// <returns>True if the receipt is valid, false otherwise</returns>
    bool VerifyReceipt(byte[] receiptBytes, X509Certificate2 rootCertificate);
}
