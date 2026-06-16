using System.Formats.Cbor;
using System.Security.Cryptography;

namespace MstVerify;

/// <summary>
/// Microsoft Azure Attestation (MAA) specific receipt fetcher
/// </summary>
public class MAAReceiptFetcher : IReceiptFetcher
{
    private readonly HttpClient _httpClient;

    public MAAReceiptFetcher(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<string> FetchServiceVersionAsync(string serviceEndpoint)
    {
        try
        {
            // Call the OpenID configuration endpoint to get the service version from headers
            var openIdUrl = $"{serviceEndpoint.TrimEnd('/')}/.well-known/openid-configuration?api-version=2018-09-01";
            
            Console.WriteLine($"Fetching service version from: {openIdUrl}");
            
            var request = new HttpRequestMessage(HttpMethod.Get, openIdUrl);
            var response = await _httpClient.SendAsync(request);

            // Extract the x-ms-maa-service-version header
            if (response.Headers.TryGetValues("x-ms-maa-service-version", out var values))
            {
                var version = values.FirstOrDefault();
                if (!string.IsNullOrEmpty(version))
                {
                    Console.WriteLine($"Found MAA service version: {version}");
                    return version;
                }
            }

            // If the specific header is not found, try other common version headers
            if (response.Headers.TryGetValues("x-ms-version", out var altValues))
            {
                var version = altValues.FirstOrDefault();
                if (!string.IsNullOrEmpty(version))
                {
                    Console.WriteLine($"Found version from x-ms-version header: {version}");
                    return version;
                }
            }

            Console.WriteLine("Warning: Could not find x-ms-maa-service-version header");
            
            // Return the request ID as fallback
            if (response.Headers.TryGetValues("x-ms-request-id", out var requestIdValues))
            {
                var requestId = requestIdValues.FirstOrDefault();
                if (!string.IsNullOrEmpty(requestId))
                {
                    return $"request-id:{requestId}";
                }
            }

            return "version-unknown";
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching service version: {ex.Message}");
            return "error-fetching-version";
        }
    }

    public async Task<byte[]> DownloadReceiptAsync(string serviceEndpoint)
    {
        try
        {
            // Try to get the attestation receipt
            // In a real scenario, you might need to:
            // 1. Generate an attestation token
            // 2. Get its receipt/proof from the service
            
            // Try common MAA endpoints for receipts/certificates
            var receiptUrl = $"{serviceEndpoint.TrimEnd('/')}/receipt";
            var response = await _httpClient.GetAsync(receiptUrl);
            
            if (response.IsSuccessStatusCode)
            {
                var bytes = await response.Content.ReadAsByteArrayAsync();
                Console.WriteLine($"Downloaded receipt from {receiptUrl}: {bytes.Length} bytes");
                return bytes;
            }

            // Alternative: try to get signing certificates
            var certsUrl = $"{serviceEndpoint.TrimEnd('/')}/certs";
            response = await _httpClient.GetAsync(certsUrl);
            
            if (response.IsSuccessStatusCode)
            {
                var bytes = await response.Content.ReadAsByteArrayAsync();
                Console.WriteLine($"Downloaded certificates from {certsUrl}: {bytes.Length} bytes");
                return bytes;
            }

            // Alternative: try to get signing keys with api-version
            var keysUrl = $"{serviceEndpoint.TrimEnd('/')}/certs?api-version=2018-09-01";
            response = await _httpClient.GetAsync(keysUrl);
            
            if (response.IsSuccessStatusCode)
            {
                var bytes = await response.Content.ReadAsByteArrayAsync();
                Console.WriteLine($"Downloaded keys from {keysUrl}: {bytes.Length} bytes");
                return bytes;
            }

            Console.WriteLine("Warning: Could not download receipt from known endpoints, creating mock receipt");
            return CreateMockCoseReceipt();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error downloading receipt: {ex.Message}");
            // Return mock receipt if download fails
            return CreateMockCoseReceipt();
        }
    }

    private byte[] CreateMockCoseReceipt()
    {
        // Create a simple COSE_Sign1 structure for demonstration
        var writer = new CborWriter();
        
        // COSE_Sign1 = [protected, unprotected, payload, signature]
        writer.WriteStartArray(4);
        
        // Protected headers (algorithm: ES256 = -7)
        var protectedWriter = new CborWriter();
        protectedWriter.WriteStartMap(1);
        protectedWriter.WriteInt32(1); // alg
        protectedWriter.WriteInt32(-7); // ES256
        protectedWriter.WriteEndMap();
        writer.WriteByteString(protectedWriter.Encode());
        
        // Unprotected headers (empty map)
        writer.WriteStartMap(0);
        writer.WriteEndMap();
        
        // Payload
        var payload = System.Text.Encoding.UTF8.GetBytes("Mock MAA Receipt");
        writer.WriteByteString(payload);
        
        // Signature (mock 64 bytes)
        var signature = new byte[64];
        RandomNumberGenerator.Fill(signature);
        writer.WriteByteString(signature);
        
        writer.WriteEndArray();
        
        return writer.Encode();
    }
}
