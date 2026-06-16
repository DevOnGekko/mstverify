using MstVerify;

string? serviceName = null;
string? operation = null;
var inputParameters = new Dictionary<string, string>(StringComparer.Ordinal);

if (args.Contains("--help") || args.Contains("-h"))
{
    PrintHelp();
    return 0;
}

for (var i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--service-name":
        case "-s":
            if (!TryReadNext(args, ref i, out serviceName))
            {
                return ExitWithError("Missing value for --service-name.");
            }
            break;
        case "--operation":
        case "-o":
            if (!TryReadNext(args, ref i, out operation))
            {
                return ExitWithError("Missing value for --operation.");
            }
            break;
        case "--param":
        case "-p":
        {
            if (!TryReadNext(args, ref i, out var raw))
            {
                return ExitWithError("Missing value for --param.");
            }
            var separator = raw.IndexOf('=');
            if (separator <= 0)
            {
                return ExitWithError(
                    $"Invalid --param value '{raw}'. Expected format NAME=VALUE.");
            }

            var name = raw[..separator];
            var value = raw[(separator + 1)..];
            inputParameters[name] = value;
            break;
        }
        default:
            if (!TryHandleCustomInputOption(args, ref i, inputParameters, out var customError))
            {
                return ExitWithError(customError ?? $"Unknown argument '{args[i]}'.");
            }
            break;
    }
}

if (string.IsNullOrWhiteSpace(serviceName))
{
    return ExitWithError("Missing required argument --service-name.");
}

if (string.IsNullOrWhiteSpace(operation))
{
    return ExitWithError("Missing required argument --operation.");
}

// Create the appropriate receipt fetcher based on service name
IReceiptFetcher receiptFetcher = serviceName.ToLowerInvariant() switch
{
    "maa" => new MAAReceiptFetcher(new HttpClient()),
    _ => throw new ArgumentException($"Unknown service: {serviceName}. Supported services: maa")
};

// Create the MST certificate verifier using Azure Code Transparency
IMstCertificateVerifier mstVerifier = new AzureMstCertificateVerifier(new HttpClient());

// Execute the operation
var verifier = new MstVerifier(receiptFetcher, mstVerifier);

try
{
    switch (operation.ToLowerInvariant())
    {
        case "verifyonline":
            return await ExecuteVerifyOnlineAsync(verifier, inputParameters);
        
        case "verifyoffline":
            return await ExecuteVerifyOfflineAsync(verifier, inputParameters);
        
        case "monitor":
            return await ExecuteMonitorAsync(verifier, inputParameters);
        
        default:
            return ExitWithError($"Unknown operation: {operation}. Supported operations: verifyonline, verifyoffline, monitor");
    }
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Unhandled error: {ex.Message}");
    return 1;
}

static async Task<int> ExecuteVerifyOnlineAsync(MstVerifier verifier, Dictionary<string, string> parameters)
{
    if (!parameters.TryGetValue("serviceEndpoint", out var serviceEndpoint) || string.IsNullOrWhiteSpace(serviceEndpoint))
    {
        return ExitWithError("Missing required parameter: -serviceEndpoint <url>");
    }

    if (!parameters.TryGetValue("mstEndpoint", out var mstEndpoint) || string.IsNullOrWhiteSpace(mstEndpoint))
    {
        return ExitWithError("Missing required parameter: -mstEndpoint <url>");
    }

    return await verifier.VerifyOnlineAsync(serviceEndpoint, mstEndpoint);
}

static async Task<int> ExecuteVerifyOfflineAsync(MstVerifier verifier, Dictionary<string, string> parameters)
{
    if (!parameters.TryGetValue("serviceReceipt", out var serviceReceipt) || string.IsNullOrWhiteSpace(serviceReceipt))
    {
        return ExitWithError("Missing required parameter: -serviceReceipt <file-path>");
    }

    if (!parameters.TryGetValue("mstEndpoint", out var mstCert) || string.IsNullOrWhiteSpace(mstCert))
    {
        return ExitWithError("Missing required parameter: -mstEndpoint <certificate-file-path>");
    }

    return await verifier.VerifyOfflineAsync(serviceReceipt, mstCert);
}

static async Task<int> ExecuteMonitorAsync(MstVerifier verifier, Dictionary<string, string> parameters)
{
    if (!parameters.TryGetValue("serviceEndpoint", out var serviceEndpoint) || string.IsNullOrWhiteSpace(serviceEndpoint))
    {
        return ExitWithError("Missing required parameter: -serviceEndpoint <url>");
    }

    if (!parameters.TryGetValue("mstEndpoint", out var mstEndpoint) || string.IsNullOrWhiteSpace(mstEndpoint))
    {
        return ExitWithError("Missing required parameter: -mstEndpoint <url>");
    }

    if (!parameters.TryGetValue("t", out var durationStr) || !int.TryParse(durationStr, out var duration))
    {
        return ExitWithError("Missing or invalid parameter: -t <duration-in-seconds>");
    }

    if (!parameters.TryGetValue("i", out var intervalStr) || !int.TryParse(intervalStr, out var interval))
    {
        return ExitWithError("Missing or invalid parameter: -i <interval-in-seconds>");
    }

    if (duration <= 0)
    {
        return ExitWithError("Duration must be greater than 0");
    }

    if (interval <= 0)
    {
        return ExitWithError("Interval must be greater than 0");
    }

    return await verifier.MonitorAsync(serviceEndpoint, mstEndpoint, duration, interval);
}

return 0;

static bool TryReadNext(string[] args, ref int index, out string value)
{
    if (index + 1 >= args.Length)
    {
        value = string.Empty;
        return false;
    }

    index++;
    value = args[index];
    return true;
}

static int ExitWithError(string message)
{
    Console.Error.WriteLine(message);
    Console.Error.WriteLine("Use --help to view usage.");
    return 1;
}

static bool TryHandleCustomInputOption(
    string[] args,
    ref int index,
    Dictionary<string, string> inputParameters,
    out string? error)
{
    error = null;
    var token = args[index];
    if (!token.StartsWith("-", StringComparison.Ordinal))
    {
        error = $"Unknown argument '{token}'.";
        return false;
    }

    var name = token.TrimStart('-');
    if (string.IsNullOrWhiteSpace(name))
    {
        error = $"Invalid argument '{token}'.";
        return false;
    }

    if (index + 1 < args.Length && !args[index + 1].StartsWith("-", StringComparison.Ordinal))
    {
        index++;
        inputParameters[name] = args[index];
        return true;
    }

    inputParameters[name] = "true";
    return true;
}

static void PrintHelp()
{
    Console.WriteLine("╔════════════════════════════════════════════════════════════════════════════╗");
    Console.WriteLine("║  MST Verify - Attestation Receipt Verification Tool                       ║");
    Console.WriteLine("║  Verify attestation service receipts against MST transparency ledger       ║");
    Console.WriteLine("╚════════════════════════════════════════════════════════════════════════════╝");
    Console.WriteLine();
    Console.WriteLine("USAGE:");
    Console.WriteLine("  mstverify -s <service> -o <operation> [options]");
    Console.WriteLine();
    Console.WriteLine("SUPPORTED SERVICES:");
    Console.WriteLine("  maa            Microsoft Azure Attestation");
    Console.WriteLine("                 Verifies MAA attestation tokens and receipts");
    Console.WriteLine();
    Console.WriteLine("OPERATIONS:");
    Console.WriteLine();
    Console.WriteLine("  verifyonline   Online Verification (Real-time)");
    Console.WriteLine("                 • Fetches service version from live endpoint");
    Console.WriteLine("                 • Downloads attestation receipt from service");
    Console.WriteLine("                 • Downloads MST root certificate");
    Console.WriteLine("                 • Verifies receipt signature using Azure Code Transparency");
    Console.WriteLine("                 Use: Real-time validation, live service checks");
    Console.WriteLine();
    Console.WriteLine("  verifyoffline  Offline Verification (Cached)");
    Console.WriteLine("                 • Verifies pre-downloaded receipt file (COSE format)");
    Console.WriteLine("                 • Uses local certificate file (PEM format)");
    Console.WriteLine("                 • No network access required");
    Console.WriteLine("                 Use: Auditing, air-gapped environments, testing");
    Console.WriteLine();
    Console.WriteLine("  monitor        Continuous Monitoring");
    Console.WriteLine("                 • Runs verifyonline repeatedly at intervals");
    Console.WriteLine("                 • Tracks success/failure statistics");
    Console.WriteLine("                 • Reports success rate and timestamps");
    Console.WriteLine("                 Use: Health monitoring, SLA validation, compliance");
    Console.WriteLine();
    Console.WriteLine("OPTIONS:");
    Console.WriteLine("  -s, --service-name <service>     Target service (required: maa)");
    Console.WriteLine("  -o, --operation <operation>      Operation to run (required)");
    Console.WriteLine("  -serviceEndpoint <url>           Service endpoint URL");
    Console.WriteLine("  -mstEndpoint <url>               MST ledger endpoint URL (online/monitor)");
    Console.WriteLine("                                   OR path to certificate file (offline)");
    Console.WriteLine("  -serviceReceipt <file>           Path to receipt file (offline only)");
    Console.WriteLine("  -t <seconds>                     Monitor duration in seconds");
    Console.WriteLine("  -i <seconds>                     Monitor interval in seconds");
    Console.WriteLine("  -h, --help                       Show this help message");
    Console.WriteLine();
    Console.WriteLine("EXAMPLES:");
    Console.WriteLine();
    Console.WriteLine("  1. Online Verification:");
    Console.WriteLine("     mstverify -s maa -o verifyonline \\");
    Console.WriteLine("       -serviceEndpoint \"https://sharedeus.eus.attest.azure.net/\" \\");
    Console.WriteLine("       -mstEndpoint \"https://prod-esrp.ledger.azure.net/\"");
    Console.WriteLine();
    Console.WriteLine("  2. Offline Verification:");
    Console.WriteLine("     mstverify -s maa -o verifyoffline \\");
    Console.WriteLine("       -serviceReceipt \"maa-receipt.cose\" \\");
    Console.WriteLine("       -mstEndpoint \"mst-root.pem\"");
    Console.WriteLine();
    Console.WriteLine("  3. Continuous Monitoring (10 minutes, every 30 seconds):");
    Console.WriteLine("     mstverify -s maa -o monitor \\");
    Console.WriteLine("       -serviceEndpoint \"https://sharedeus.eus.attest.azure.net/\" \\");
    Console.WriteLine("       -mstEndpoint \"https://prod-esrp.ledger.azure.net/\" \\");
    Console.WriteLine("       -t 600 -i 30");
    Console.WriteLine();
    Console.WriteLine("EXIT CODES:");
    Console.WriteLine("  0    Success - Receipt verification passed");
    Console.WriteLine("  1    Failure - Verification failed or error occurred");
    Console.WriteLine();
    Console.WriteLine("For more information, visit: https://github.com/DevOnGekko/mstverify");
    Console.WriteLine();
}
