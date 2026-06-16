# MST Verify

A command-line tool for verifying service receipts using MST (Microsoft Signing Transparency) root certificates. This tool helps ensure the integrity and authenticity of attestation receipts by validating them against trusted root certificates from the Microsoft Signing Transparency service.

## What is MST?

MST (Microsoft Signing Transparency) is Microsoft's transparency service that provides cryptographic proof of signing operations. It creates an immutable, append-only ledger backed by a Confidential Consortium Framework (CCF) ledger where each entry is cryptographically linked. This tool verifies that service receipts are properly signed and recorded in the MST ledger.

## Features

- **✓ Verify Online**: Fetch service version, download receipts, and verify against live MST service
- **✓ Verify Offline**: Verify pre-downloaded receipts using local certificate files
- **✓ Monitor**: Continuously monitor and verify MST receipts over time with detailed statistics
- **✓ Multi-Service Support**: Pluggable architecture for different services (currently supports MAA)
- **✓ Azure Code Transparency**: Uses official Microsoft libraries for cryptographic verification

## Supported Services

| Service | Description | Service Name |
|---------|-------------|--------------|
| **MAA** | Microsoft Azure Attestation | `maa` |

### MAA (Microsoft Azure Attestation)

The MAA implementation:
- Fetches service version from the `x-ms-maa-service-version` header
- Calls the OpenID configuration endpoint: `/.well-known/openid-configuration?api-version=2018-09-01`
- Downloads receipts from standard MAA endpoints

## Installation

```bash
dotnet build
```

## Quick Start

### Basic Verification (Online)

Verify an attestation service receipt against the live MST service:

```bash
mstverify -s maa -o verifyonline \
  -serviceEndpoint "https://sharedeus.eus.attest.azure.net/" \
  -mstEndpoint "https://prod-esrp.ledger.azure.net/"
```

**What this does:**
1. Connects to the MAA service and retrieves the current service version from HTTP headers
2. Downloads the attestation receipt from the receipt repository (MAR)
3. Downloads the MST root certificate from the transparency ledger
4. Verifies the receipt's cryptographic signature using Azure Code Transparency
5. Reports success or failure with detailed output

**When to use:** For real-time verification against live services to ensure current receipts are valid.

---

### Offline Verification

Verify a previously downloaded receipt using a local certificate file:

```bash
mstverify -s maa -o verifyoffline \
  -serviceReceipt "maa-receipt.cose" \
  -mstEndpoint "mst-root.pem"
```

**What this does:**
1. Reads the receipt file from disk (COSE format)
2. Reads the root certificate from disk (PEM format)
3. Verifies the receipt signature without network access
4. Validates certificate time validity
5. Reports verification results

**When to use:** For auditing archived receipts, testing in air-gapped environments, or when network access is unavailable.

**Requirements:**
- Receipt file must be in COSE (CBOR Object Signing and Encryption) format
- Certificate file must be in PEM format
- Both files must be accessible on the local filesystem

---

### Continuous Monitoring

Continuously verify receipts over a period of time with detailed statistics:

```bash
mstverify -s maa -o monitor \
  -serviceEndpoint "https://sharedeus.eus.attest.azure.net/" \
  -mstEndpoint "https://prod-esrp.ledger.azure.net/" \
  -t 600 -i 20
```

**What this does:**
1. Runs the online verification flow repeatedly
2. Executes verification every 20 seconds (interval)
3. Continues for 600 seconds total (10 minutes)
4. Tracks and reports success/failure counts
5. Calculates and displays success rate statistics
6. Timestamps each verification attempt

**When to use:** 
- Monitoring service health and availability
- Detecting intermittent verification failures
- Compliance and audit logging
- Service level agreement (SLA) validation

**Parameters:**
- `-t <seconds>`: Total monitoring duration (e.g., 600 = 10 minutes, 3600 = 1 hour)
- `-i <seconds>`: Interval between checks (e.g., 20 = every 20 seconds, 60 = every minute)

**Output includes:**
- Iteration number and timestamp
- Real-time success/failure for each check
- Running totals and success rate percentage
- Final summary with total iterations and overall success rate

## Command-Line Options

| Option | Description |
|--------|-------------|
| `-s, --service-name` | Target service name (required, e.g., "maa") |
| `-o, --operation` | Operation to run: `verifyonline`, `verifyoffline`, or `monitor` |
| `-serviceEndpoint` | Service endpoint URL |
| `-mstEndpoint` | MST service endpoint URL (for online/monitor) or path to certificate file (for offline) |
| `-serviceReceipt` | Path to service receipt file (COSE format, for offline verification) |
| `-t` | Duration in seconds (for monitor operation) |
| `-i` | Interval in seconds between checks (for monitor operation) |
| `-h, --help` | Show help message |

## How It Works

### Online Verification Flow (verifyonline)

```
┌─────────────────────────────────────────────────────────────────┐
│ Step 1: Fetch Service Version                                  │
│ → Calls: /.well-known/openid-configuration?api-version=...     │
│ → Extracts: x-ms-maa-service-version header                    │
│ → Result: Version string (e.g., "1.11.03371.5702")             │
└─────────────────────────────────────────────────────────────────┘
                           ↓
┌─────────────────────────────────────────────────────────────────┐
│ Step 2: Download Receipt                                        │
│ → Tries multiple endpoints (/receipt, /certs, /certs?api-...)  │
│ → Downloads: COSE-encoded receipt bytes                        │
│ → Format: CBOR/COSE_Sign1 structure                            │
└─────────────────────────────────────────────────────────────────┘
                           ↓
┌─────────────────────────────────────────────────────────────────┐
│ Step 3: Download MST Root Certificate                          │
│ → Tries: /app/governance/serviceCertificate                    │
│          /app/governance/constitution                           │
│          /node/network                                          │
│          /app/did                                               │
│ → Downloads: X.509 certificate in PEM format                   │
└─────────────────────────────────────────────────────────────────┘
                           ↓
┌─────────────────────────────────────────────────────────────────┐
│ Step 4: Verify Receipt                                          │
│ → Parses COSE_Sign1 structure (protected headers, payload, sig)│
│ → Verifies signature using Azure Code Transparency             │
│ → Validates certificate time validity                           │
│ → Result: ✓ PASSED or ✗ FAILED                                 │
└─────────────────────────────────────────────────────────────────┘
```

### Offline Verification Flow (verifyoffline)

```
┌─────────────────────────────────────────────────────────────────┐
│ Step 1: Load Receipt File                                       │
│ → Reads: Local COSE file from filesystem                       │
│ → Validates: File exists and is readable                       │
└─────────────────────────────────────────────────────────────────┘
                           ↓
┌─────────────────────────────────────────────────────────────────┐
│ Step 2: Load Certificate File                                   │
│ → Reads: Local PEM certificate from filesystem                 │
│ → Parses: X.509 certificate structure                          │
└─────────────────────────────────────────────────────────────────┘
                           ↓
┌─────────────────────────────────────────────────────────────────┐
│ Step 3: Verify Receipt                                          │
│ → Same verification process as online mode                     │
│ → No network access required                                   │
│ → Result: ✓ PASSED or ✗ FAILED                                 │
└─────────────────────────────────────────────────────────────────┘
```

### Monitor Mode Flow (monitor)

```
┌───────────────────────────────────────────────────────────────┐
│ Initialize: Start time, counters, duration, interval         │
└───────────────────────────────────────────────────────────────┘
                         ↓
              ┌──────────────────┐
              │   Main Loop      │
              │ (Until duration) │
              └──────────────────┘
                         ↓
         ┌───────────────────────────────┐
         │ Iteration N: [Timestamp]      │
         │ → Execute verifyonline flow   │
         │ → Record success/failure      │
         │ → Update statistics           │
         │ → Display summary             │
         └───────────────────────────────┘
                         ↓
         ┌───────────────────────────────┐
         │ Wait for interval seconds     │
         └───────────────────────────────┘
                         ↓
              ┌──────────────────┐
              │  Final Summary   │
              │ • Total runs     │
              │ • Success count  │
              │ • Failure count  │
              │ • Success rate % │
              └──────────────────┘
```

## MST Receipt Verification

The tool uses **Azure Code Transparency** (`Azure.Security.CodeTransparency` NuGet package) for MST receipt verification:
- Downloads the root certificate from the MST endpoint
- Verifies COSE_Sign1 structures according to RFC 8152
- Validates certificate chain and time validity
- Supports RSA and ECDSA signature algorithms

## Exit Codes

- `0`: Success - receipt verified successfully
- `1`: Failure - verification failed or error occurred

## Requirements

- .NET 8.0 or later
- Internet connection (for online and monitor operations)

## Dependencies

- `System.Formats.Cbor` - For COSE/CBOR parsing
- `Azure.Security.CodeTransparency` - For MST receipt verification and certificate handling
- .NET built-in cryptography libraries for certificate and signature verification

## Architecture

The tool uses a pluggable architecture to support multiple attestation services and verification methods:

### Core Components

1. **`IReceiptFetcher` Interface**: Defines the contract for service-specific implementations
   - `FetchServiceVersionAsync()`: Retrieves the service version/build number
   - `DownloadReceiptAsync()`: Downloads the receipt from the service

2. **`IMstCertificateVerifier` Interface**: Defines the contract for MST verification
   - `DownloadRootCertificateAsync()`: Downloads the root certificate from MST endpoint
   - `VerifyReceipt()`: Verifies the receipt against the root certificate

3. **Service Implementations**:
   - `MAAReceiptFetcher`: Microsoft Azure Attestation implementation
   - `AzureMstCertificateVerifier`: Azure Code Transparency-based MST verification

4. **`MstVerifier`**: Core orchestration logic that coordinates the verification flow

### Adding New Services

To add support for a new attestation service:

1. Create a new class that implements `IReceiptFetcher`:
```csharp
public class MyServiceReceiptFetcher : IReceiptFetcher
{
    public async Task<string> FetchServiceVersionAsync(string serviceEndpoint)
    {
        // Implement service-specific version fetching
    }

    public async Task<byte[]> DownloadReceiptAsync(string serviceEndpoint)
    {
        // Implement service-specific receipt downloading
    }
}
```

2. Register the new service in `Program.cs`:
```csharp
IReceiptFetcher receiptFetcher = serviceName.ToLowerInvariant() switch
{
    "maa" => new MAAReceiptFetcher(new HttpClient()),
    "myservice" => new MyServiceReceiptFetcher(new HttpClient()),
    _ => throw new ArgumentException($"Unknown service: {serviceName}")
};
```

## Example: MAA Service Version Retrieval

The MAA implementation retrieves the service version from the HTTP response header:

```bash
# Request
GET https://sharedeus.eus.attest.azure.net/.well-known/openid-configuration?api-version=2018-09-01

# Response Headers
Date: Mon, 30 Mar 2026 17:50:45 GMT
Server: Kestrel
x-ms-request-id: 00-3a6b47dd2162e1459d42006bd5a81891-0000000000000000-00
x-ms-maa-service-version: 1.11.03371.5702
```

The version `1.11.03371.5702` is extracted from the `x-ms-maa-service-version` header.

## MST Certificate Download

The `AzureMstCertificateVerifier` implementation attempts to download the root certificate from multiple MST endpoints:

1. **Service Certificate Endpoint**: `/app/governance/serviceCertificate`
2. **Constitution Endpoint**: `/app/governance/constitution`
3. **Network Info Endpoint**: `/node/network` (looks for `service_certificate`, `serviceCertificate`, `cert`, or `certificate` properties)
4. **DID Endpoint**: `/app/did`

The verifier tries each endpoint in sequence until it successfully retrieves a valid X.509 certificate in PEM format.

Example MST endpoint: `https://prod-esrp.ledger.azure.net/`

## Verification Process

The complete verification flow:

1. **Service-specific receipt fetching** (via `IReceiptFetcher`)
   - Fetch service version from headers
   - Download receipt from service endpoints

2. **MST certificate download** (via `IMstCertificateVerifier`)
   - Download root certificate from MST endpoint
   - Parse and validate certificate format

3. **Receipt verification** (via Azure Code Transparency)
   - Verify COSE_Sign1 structure
   - Validate signature against root certificate
   - Check certificate validity period

## Troubleshooting

### Common Issues and Solutions

#### Error: "Missing required parameter: -serviceEndpoint"

**Cause:** The `-serviceEndpoint` parameter is required for `verifyonline` and `monitor` operations.

**Solution:**
```bash
# Add the service endpoint URL
mstverify -s maa -o verifyonline \
  -serviceEndpoint "https://sharedeus.eus.attest.azure.net/" \
  -mstEndpoint "https://prod-esrp.ledger.azure.net/"
```

---

#### Error: "Receipt file not found"

**Cause:** The specified receipt file does not exist or the path is incorrect.

**Solution:**
- Verify the file path is correct: `ls -la maa-receipt.cose`
- Ensure the file has proper read permissions
- Use absolute path if relative path fails: `/full/path/to/receipt.cose`

---

#### Error: "Unable to download root certificate from MST endpoint"

**Cause:** Network connectivity issues or MST endpoint is unreachable.

**Solution:**
1. Check network connectivity: `ping prod-esrp.ledger.azure.net`
2. Verify the MST endpoint URL is correct
3. Check if firewall is blocking outbound HTTPS traffic
4. Try alternative MST endpoints if available

---

#### Warning: "Could not find x-ms-maa-service-version header"

**Cause:** MAA service endpoint may not be returning the expected header.

**Solution:**
- Verify you're using the correct MAA endpoint URL
- Check if the service is running: try accessing `/.well-known/openid-configuration`
- The tool will continue with fallback version information

---

#### Verification Failed: Certificate not currently valid

**Cause:** The MST root certificate is expired or not yet valid.

**Solution:**
1. Check system time/date is correct: `date`
2. Download a fresh certificate from the MST endpoint
3. Verify the certificate validity period: `openssl x509 -in cert.pem -noout -dates`

---

#### Network Timeouts

**Cause:** Slow network or service is not responding.

**Solution:**
- Check internet connectivity
- Try a different network connection
- Verify service endpoints are accessible
- For monitor mode, increase the interval: `-i 60` (60 seconds)

---

### Debug Information

To get more detailed output, the tool automatically prints:
- Service version information
- Receipt size in bytes
- Certificate subject and issuer details
- Certificate validity period
- Signature verification results

### Getting Help

If you encounter issues not covered here:

1. Check the exit code: `echo $?` (Linux/Mac) or `echo $LASTEXITCODE` (PowerShell)
   - `0` = Success
   - `1` = Failure

2. Review the console output for specific error messages

3. Verify all prerequisites are installed:
   ```bash
   dotnet --version  # Should be 8.0 or later
   ```

4. Check NuGet packages are restored:
   ```bash
   dotnet restore
   dotnet build
   ```

5. Report issues at: https://github.com/DevOnGekko/mstverify/issues

### Verbose Output Example

```
=== Starting Online Verification ===
Service Endpoint: https://sharedeus.eus.attest.azure.net/
MST Endpoint: https://prod-esrp.ledger.azure.net/

Step 1: Fetching service version...
Fetching service version from: https://sharedeus.eus.attest.azure.net/.well-known/openid-configuration?api-version=2018-09-01
Found MAA service version: 1.11.03371.5702
Service Version: 1.11.03371.5702

Step 2: Downloading receipt from service...
Receipt downloaded: 1024 bytes

Step 3: Downloading root certificate from MST endpoint...
Downloading root certificate from MST endpoint: https://prod-esrp.ledger.azure.net/
Downloaded service certificate from /app/governance/serviceCertificate
Root certificate downloaded: CN=MST Service

Step 4: Verifying MST receipt...
Verifying receipt using Azure Code Transparency...
Root Certificate Subject: CN=MST Service
Root Certificate Valid From: 2025-01-01 00:00:00
Root Certificate Valid To: 2027-01-01 00:00:00
✓ Receipt structure appears valid
✓ Certificate is currently valid
✓ Receipt verification completed successfully
✓ MST receipt verification PASSED
```
