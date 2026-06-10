using System.Text.Json;

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
            return ExitWithError($"Unknown argument '{args[i]}'.");
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

var payload = new
{
    serviceName,
    operation,
    inputParameters
};

Console.WriteLine(JsonSerializer.Serialize(payload, new JsonSerializerOptions
{
    WriteIndented = true
}));

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

static void PrintHelp()
{
    Console.WriteLine("Usage:");
    Console.WriteLine("  mstverify --service-name|-s <service-name> --operation|-o <operation> [--param|-p NAME=VALUE ...]");
    Console.WriteLine();
    Console.WriteLine("Options:");
    Console.WriteLine("  --service-name, -s <service-name>   Target service name (required)");
    Console.WriteLine("  --operation, -o <operation>         Operation to run (required)");
    Console.WriteLine("  --param, -p NAME=VALUE              Input parameter (repeatable)");
    Console.WriteLine("  -h, --help                      Show help");
}
