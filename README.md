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

Example:

```bash
dotnet run --project ./mstverify.csproj -- \
  --service-name signing-transparency \
  --operation verify \
  --param tenantId=contoso \
  --param artifactDigest=sha256:abcd
```
