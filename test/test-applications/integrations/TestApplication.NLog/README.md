# NLog OpenTelemetry Integration Test Application

This test application demonstrates the integration between NLog and OpenTelemetry auto-instrumentation. It showcases various logging scenarios to verify that the NLog bridge correctly forwards log events to OpenTelemetry.

## Features Demonstrated

1. **Direct NLog Logging**: Using NLog's static logger methods
2. **Microsoft.Extensions.Logging Integration**: Using the generic host with NLog provider
3. **Structured Logging**: Message templates with parameters
4. **Exception Logging**: Logging exceptions with full stack traces
5. **Custom Properties**: Adding custom properties to log entries using scopes
6. **Service-based Logging**: Logging from dependency injection services
7. **Async Logging**: Logging during asynchronous operations

## Prerequisites

- .NET 8.0 or later
- OpenTelemetry .NET Auto-Instrumentation with NLog bridge enabled

## Configuration

The application uses two configuration approaches:

### 1. NLog Configuration File (`nlog.config`)

The `nlog.config` file defines targets for:
- File logging (all levels)
- Colored console logging (Info and above)
- Proper formatting and filtering

### 2. Programmatic Configuration

The application also demonstrates programmatic NLog configuration including:
- Global diagnostic context properties
- Microsoft.Extensions.Logging integration
- Custom logger categories

## Running the Application

### With OpenTelemetry Auto-Instrumentation

To test the NLog bridge integration:

1. Build the application:
   ```bash
   dotnet build
   ```

2. Set the required environment variables:
   ```bash
   export OTEL_DOTNET_AUTO_LOGS_ENABLED=true
   export OTEL_DOTNET_AUTO_LOGS_ENABLE_NLOG_BRIDGE=true
   export OTEL_LOGS_EXPORTER=console
   ```

3. Run with OpenTelemetry auto-instrumentation:
   ```bash
   # Assuming the auto-instrumentation is properly installed
   dotnet run
   ```

### Without Auto-Instrumentation (for comparison)

You can also run the application without auto-instrumentation to see the difference:

```bash
dotnet run
```

## Expected Output

When run with OpenTelemetry auto-instrumentation enabled, you should see:

1. **Console Output**: Standard NLog console target output with colored formatting
2. **File Output**: Detailed logs written to `logs/nlog-demo-{date}.log`
3. **OpenTelemetry Output**: If console exporter is enabled, you'll see OpenTelemetry log records in the console

## Verification

To verify the integration is working:

1. **Check Console Output**: Look for both NLog console output and OpenTelemetry log records
2. **Check Log Files**: Examine the generated log files for completeness
3. **Check Structured Data**: Verify that structured logging parameters are captured
4. **Check Exception Data**: Ensure exceptions are properly serialized
5. **Check Custom Properties**: Confirm that scope properties are included

## Troubleshooting

### NLog Bridge Not Working

If you don't see OpenTelemetry log records:

1. Verify environment variables are set:
   ```bash
   echo $OTEL_DOTNET_AUTO_LOGS_ENABLED
   echo $OTEL_DOTNET_AUTO_LOGS_ENABLE_NLOG_BRIDGE
   ```

2. Check that auto-instrumentation is properly installed and loaded

3. Ensure NLog version is supported (4.0.0 - 6.*.*)

### Missing Log Data

If some log data is missing:

1. Check NLog configuration for minimum log levels
2. Verify that the OpenTelemetry target is being injected
3. Check for any error messages in the application output

## Integration Points

This application exercises the following integration points:

- **Target Injection**: The OpenTelemetry target should be automatically added to NLog's target collection
- **Log Event Processing**: All log events should be captured and converted to OpenTelemetry format
- **Level Mapping**: NLog levels should be correctly mapped to OpenTelemetry severity levels
- **Property Extraction**: Custom properties and structured logging parameters should be preserved
- **Exception Handling**: Exceptions should be properly serialized and included
- **Trace Context**: Active trace context should be included in log records

## Notes

- The application creates an activity to demonstrate trace context integration
- Global diagnostic context properties are set to show context propagation
- Both direct NLog usage and Microsoft.Extensions.Logging usage are demonstrated
- The application includes proper error handling and graceful shutdown 