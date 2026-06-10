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

Example:

```bash
dotnet run --project ./mstverify.csproj -- \
  -s signing-transparency \
  -o verify \
  -p tenantId=contoso \
  -p artifactDigest=sha256:abcd
```
