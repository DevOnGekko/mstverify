# mstverify
CLI tool for verification of Microsoft Signing transparency 

## Usage

```bash
dotnet run --project ./mstverify.csproj -- \
  --service-name <service-name> \
  --operation <operation> \
  --param <name>=<value> \
  --param <name>=<value>
```

Short aliases:
- `-s` for `--service-name`
- `-o` for `--operation`
- `-p` for `--param`
- Direct named inputs are also supported as `-<name> <value>` (example: `-maaEndpoint <url>`)
- Bare custom flags are supported as `-<flag>` and stored as `"true"` in `inputParameters`

Example:

```bash
dotnet run --project ./mstverify.csproj -- \
  -s signing-transparency \
  -o verify \
  -p tenantId=contoso \
  -p artifactDigest=sha256:abcd
```

Requested command forms:

```bash
dotnet run --project ./mstverify.csproj -- -s maa -o verifyonline -maaEndpoint "https://sharedeus.eus.attest.azure.net/" -mstEndpoint "https://prod-esrp.ledger.azure.net/"
dotnet run --project ./mstverify.csproj -- -s maa -o verifyoffline -maaReceipt "maa-receipt.cose" -mstEndpoint "mst-root.pem"
dotnet run --project ./mstverify.csproj -- -s maa -o monitor -t 600 -m
```
