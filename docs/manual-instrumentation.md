# Manually instrument a .NET application

The auto-instrumentation provides a base you can build on by adding your own
custom instrumentation. By using both instrumentation approaches, you'll be
able to present a more detailed representation of the logic and functionality
of your application, clients, and framework.

To create your custom traces you should execute following steps.

1. Add the `System.Diagnostics.DiagnosticSource` dependency to your project:

    ```xml
    <PackageReference Include="System.Diagnostics.DiagnosticSource" Version="6.0.0" />
    ```

1. Create `ActivitySource` instance

    ```csharp
        private static readonly ActivitySource RegisteredActivity = new ActivitySource("Examples.ManualInstrumentations.Registered");
    ```

1. Create `Activity` and optionally set tags

    ```csharp
            using (var activity = RegisteredActivity.StartActivity("Main"))
            {
                activity?.SetTag("foo", "bar1");
                // your logic for Main activity
            }
    ```

1. Register your `ActivitySource` in OpenTelemetry.AutoInstrumentation
by setting enironemental variable `OTEL_DOTNET_AUTO_ADDITIONAL_SOURCES`.
Value can be set either to very specific value `Examples.ManualInstrumentations.Registerd`
or to `Examples.ManualInstrumentations.*` which registers whole prefix.

You can see example console application with auto (`HttpClient`) and manual
instrumentation [here](..\examples\ManualInstrumenation\Examples.ManualInstrumentations).

Please note that `Activity` created for `NonRegistered.ManualInstrumentations`
`ActivitySoruce` will be not handled by the OpenTelemetry AutoInstrumentation.

Further reading:

- [OpenTelemetry.io documentation for .NET Manual Instrumentation](https://opentelemetry.io/docs/instrumentation/net/manual/)
