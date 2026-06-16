namespace MstVerify;

public class MstVerifier
{
    private readonly IReceiptFetcher _receiptFetcher;
    private readonly IMstCertificateVerifier _mstVerifier;

    public MstVerifier(IReceiptFetcher receiptFetcher, IMstCertificateVerifier mstVerifier)
    {
        _receiptFetcher = receiptFetcher;
        _mstVerifier = mstVerifier;
    }

    public async Task<int> VerifyOnlineAsync(string serviceEndpoint, string mstEndpoint)
    {
        try
        {
            Console.WriteLine("=== Starting Online Verification ===");
            Console.WriteLine($"Service Endpoint: {serviceEndpoint}");
            Console.WriteLine($"MST Endpoint: {mstEndpoint}");
            Console.WriteLine();

            // Step 1: Fetch service version
            Console.WriteLine("Step 1: Fetching service version...");
            var serviceVersion = await _receiptFetcher.FetchServiceVersionAsync(serviceEndpoint);
            Console.WriteLine($"Service Version: {serviceVersion}");
            Console.WriteLine();

            // Step 2: Download receipt from service
            Console.WriteLine("Step 2: Downloading receipt from service...");
            var receiptBytes = await _receiptFetcher.DownloadReceiptAsync(serviceEndpoint);
            Console.WriteLine($"Receipt downloaded: {receiptBytes.Length} bytes");
            Console.WriteLine();

            // Step 3: Download root certificate from MST endpoint
            Console.WriteLine("Step 3: Downloading root certificate from MST endpoint...");
            var rootCertificate = await _mstVerifier.DownloadRootCertificateAsync(mstEndpoint);
            Console.WriteLine($"Root certificate downloaded: {rootCertificate.Subject}");
            Console.WriteLine();

            // Step 4: Verify the receipt
            Console.WriteLine("Step 4: Verifying MST receipt...");
            var isValid = _mstVerifier.VerifyReceipt(receiptBytes, rootCertificate);
            
            if (isValid)
            {
                Console.WriteLine("✓ MST receipt verification PASSED");
                return 0;
            }
            else
            {
                Console.WriteLine("✗ MST receipt verification FAILED");
                return 1;
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error during online verification: {ex.Message}");
            return 1;
        }
    }

    public async Task<int> VerifyOfflineAsync(string serviceReceiptPath, string mstCertPath)
    {
        try
        {
            Console.WriteLine("=== Starting Offline Verification ===");
            Console.WriteLine($"Service Receipt: {serviceReceiptPath}");
            Console.WriteLine($"MST Certificate: {mstCertPath}");
            Console.WriteLine();

            // Step 1: Read receipt file
            Console.WriteLine("Step 1: Reading MST receipt file...");
            if (!File.Exists(serviceReceiptPath))
            {
                Console.Error.WriteLine($"Receipt file not found: {serviceReceiptPath}");
                return 1;
            }
            var receiptBytes = await File.ReadAllBytesAsync(serviceReceiptPath);
            Console.WriteLine($"Receipt loaded: {receiptBytes.Length} bytes");
            Console.WriteLine();

            // Step 2: Read certificate file
            Console.WriteLine("Step 2: Reading root certificate file...");
            if (!File.Exists(mstCertPath))
            {
                Console.Error.WriteLine($"Certificate file not found: {mstCertPath}");
                return 1;
            }
            var rootCertificate = new X509Certificate2(mstCertPath);
            Console.WriteLine($"Certificate loaded: {rootCertificate.Subject}");
            Console.WriteLine();

            // Step 3: Verify the receipt
            Console.WriteLine("Step 3: Verifying MST receipt...");
            var isValid = _mstVerifier.VerifyReceipt(receiptBytes, rootCertificate);
            
            if (isValid)
            {
                Console.WriteLine("✓ MST receipt verification PASSED");
                return 0;
            }
            else
            {
                Console.WriteLine("✗ MST receipt verification FAILED");
                return 1;
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error during offline verification: {ex.Message}");
            return 1;
        }
    }

    public async Task<int> MonitorAsync(string serviceEndpoint, string mstEndpoint, int durationSeconds, int intervalSeconds)
    {
        try
        {
            Console.WriteLine("=== Starting Monitor Mode ===");
            Console.WriteLine($"Service Endpoint: {serviceEndpoint}");
            Console.WriteLine($"MST Endpoint: {mstEndpoint}");
            Console.WriteLine($"Duration: {durationSeconds} seconds");
            Console.WriteLine($"Interval: {intervalSeconds} seconds");
            Console.WriteLine();

            var startTime = DateTime.UtcNow;
            var endTime = startTime.AddSeconds(durationSeconds);
            var iteration = 0;
            var successCount = 0;
            var failureCount = 0;

            while (DateTime.UtcNow < endTime)
            {
                iteration++;
                Console.WriteLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Iteration #{iteration}");
                
                var result = await VerifyOnlineAsync(serviceEndpoint, mstEndpoint);
                
                if (result == 0)
                {
                    successCount++;
                }
                else
                {
                    failureCount++;
                }

                Console.WriteLine();
                Console.WriteLine($"Summary - Success: {successCount}, Failures: {failureCount}");
                Console.WriteLine(new string('-', 60));
                Console.WriteLine();

                var remainingTime = endTime - DateTime.UtcNow;
                if (remainingTime.TotalSeconds > intervalSeconds)
                {
                    Console.WriteLine($"Waiting {intervalSeconds} seconds until next check...");
                    await Task.Delay(TimeSpan.FromSeconds(intervalSeconds));
                }
                else if (remainingTime.TotalSeconds > 0)
                {
                    Console.WriteLine($"Waiting {remainingTime.TotalSeconds:F0} seconds until monitoring ends...");
                    await Task.Delay(remainingTime);
                }
            }

            Console.WriteLine("=== Monitor Complete ===");
            Console.WriteLine($"Total Iterations: {iteration}");
            Console.WriteLine($"Successful: {successCount}");
            Console.WriteLine($"Failed: {failureCount}");
            Console.WriteLine($"Success Rate: {(successCount * 100.0 / iteration):F2}%");

            return failureCount > 0 ? 1 : 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error during monitoring: {ex.Message}");
            return 1;
        }
    }
}
