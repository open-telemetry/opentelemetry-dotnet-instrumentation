# Getting Started with dotnet-monitor logs collection

- [Produce logs from the application](#produce-logs-from-the-application)
- [Collect logs using dotnet-monitor](#collect-logs-using-dotnet-monitor)
  - [Configuration](#configuration)
  - [Running dotnet-monitor](#running-dotnet-monitor)
  - [Running the application](#running-the-application)
  - [Validate the logs](#validate-the-logs)
- [Learn More](#learn-more)

## Produce logs from the application

Create a new console application and run it:

```sh
dotnet new console --output getting-started-dotnet-monitor-logs
cd getting-started-dotnet-monitor-logs
dotnet run
```

Now copy the code from [Program.cs](./Program.cs)

For learning purposes, use a while-loop to log the same message:

```csharp
Console.WriteLine("Press any key to exit");
while (!Console.KeyAvailable)
{
    logger.FoodPriceChanged("artichoke", 9.99);

    using (logger.BeginScope(new List<KeyValuePair<string, object>>
            {
                new("store", "Seattle"),
            }))
    {
        logger.FoodPriceChanged("truffle", 999.99);
    }

    logger.FoodRecallNotice(
        brandName: "Contoso",
        productDescription: "Salads",
        productType: "Food & Beverages",
        recallReasonDescription: "due to a possible health risk from Listeria monocytogenes",
        companyName: "Contoso Fresh Vegetables, Inc.");

    Thread.Sleep(300);
}
```

## Collect logs using dotnet-monitor

Follow the [install
steps](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/dotnet-monitor#install)
to download the dotnet-monitor.

### Configuration

1. **Configure dotnet-monitor API key**: To secure access to the dotnet-monitor
  endpoints, you can set up an API key authentication by following the steps
  outlined in [Configuring API Key
  Authentication](https://github.com/dotnet/dotnet-monitor/blob/main/documentation/api-key-setup.md#configuring-api-key-authentication).
  If your use case is limited to a test environment, you might opt to bypass API
  key configuration by using the `--no-auth` switch when running dotnet-monitor.
  Learn more about dotnet-monitor authentication
  [here](https://github.com/dotnet/dotnet-monitor/blob/1beca4d497da1e60985394fe7d1195c0663f7095/documentation/authentication.md?plain=1#L115).

2. **Configure Egress for Logs Collection**:  Define egress settings in a
   `settings.json` file to specify where logs should be stored, such as a local
   directory or Azure Blob Storage. Here's a configuration for file system
   egress:

   ```json
   {
     "$schema": "https://aka.ms/dotnet-monitor-schema",
     "CollectionRuleDefaults": {
      "Actions": {
        "Egress": "artifacts"
      }
     },
     "Egress": {
      "FileSystem": {
        "artifacts": {
         "DirectoryPath": "path/to/artifacts",
         "IntermediateDirectoryPath": "path/to/intermediateArtifacts",
         "CopyBufferSize": 500
        }
      },
      "console": {
        "type": "console"
      }
     },
     "CollectionRules": {
      "AssemblyLoadTraceOnStartup": {
        "Trigger": {
         "Type": "Startup"
        },
        "Actions": [
         {
           "Type": "CollectLogs",
           "Settings": {
            "Egress": "artifacts",
            "DefaultLevel": "Warning",
            "UseAppFilters": false,
            "Duration": "00:00:30",
            "Format": "JsonSequence"
           }
         }
        ]
      }
     }
   }
   ```

   When starting dotnet-monitor, specify the path to this settings.json file using
   the `--configuration-file-path` switch.

3. **Diagnostic Port Configuration**: dotnet monitor communicates with .NET
   processes through their diagnostic port (--diagnostic-port). By default, .NET
   processes listen on a platform-native transport-named pipes on Windows and
   Unix-domain sockets on *nix—in a well-known location. For detailed
   information on configuring the diagnostic port, please refer to [Diagnostic
   Port
   Configuration](https://github.com/dotnet/dotnet-monitor/blob/main/documentation/configuration/diagnostic-port-configuration.md).

### Running dotnet-monitor

Run dotnet-monitor in a command prompt or terminal. If you have configured an
API key, ensure to use it. If you are running in a test environment and prefer
not to use authentication, you can start dotnet-monitor with the `--no-auth`
switch. dotnet monitor communicates via .NET processes through their diagnostic
port. In the default configuration, .NET processes listen on a platform native
transport (named pipes on Windows/Unix-domain sockets on *nix) in a well-known
location. It is possible to change this behavior and have .NET processes connect
to dotnet monitor using --diagnostic-port switch. This is the recommended
approach for using collection rules.

```bash
dotnet-monitor collect --no-auth --configuration-file-path path/to/settings.json --diagnostic-port /diag/port.sock
```

Or, if you have set up an API key:

```bash
dotnet-monitor collect --configuration-file-path path/to/settings.json --diagnostic-port /diag/port.sock
```

### Running the application

Before starting your application, set the `DOTNET_DiagnosticPorts` environment
variable to `/diag/port.sock`. This step informs the .NET runtime where to expose
diagnostic information, which is essential for dotnet monitor to collect
diagnostics.

### Validate the logs

Check the logs in the specified artifacts folder in your settings.json file to
validate the log collection.
