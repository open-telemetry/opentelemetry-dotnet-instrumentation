# Log to trace correlation

OpenTelemetry .NET SDK automatically correlates logs to trace data.
When logs are emitted in the context of an active trace, trace context
[fields](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/logs/data-model.md#trace-context-fields)
are automatically populated.

The following are logs produced by the sample console
[application](../examples/demo/Service/Program.cs):

```json
"logRecords": [
    {
        "timeUnixNano": "1679392614538226700",
        "severityNumber": 9,
        "severityText": "Information",
        "body": {
            "stringValue": "Success! Today is: {Date:MMMM dd, yyyy}"
        },
        "attributes": [
            {
                "key": "dotnet.ilogger.category",
                "value": {
                    "stringValue": "Program"
                }
            },
            {
                "key": "Date",
                "value": {
                    "stringValue": "03/21/2023 09:56:54 +00:00"
                }
            }
        ],
        "flags": 1,
        "traceId": "21df288eada1ce4ace6c40f39a6d7ce1",
        "spanId": "a80119e5a05fed5a"
    }
]
```

Further reading:

- [OpenTelemetry .NET SDK](https://github.com/open-telemetry/opentelemetry-dotnet/tree/main/docs/logs/correlation)
- [OpenTelemetry Specification](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/logs/data-model.md#trace-context-fields)
