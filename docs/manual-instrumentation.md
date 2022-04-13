# Manually instrument a .NET application

The automatic instrumentation provides a base you can build on by adding your own
manual instrumentation. By using both automatic and manual instrumentation, you can
better instrument the logic and functionality of your applications, clients, and frameworks.

To create your custom traces manually, follow these steps:

1. Add the `System.Diagnostics.DiagnosticSource` dependency to your project:

    ```xml
    <PackageReference Include="System.Diagnostics.DiagnosticSource" Version="6.0.0" />
    ```

2. Create an `ActivitySource` instance:

    ```csharp
        private static readonly ActivitySource RegisteredActivity = new ActivitySource("Examples.ManualInstrumentations.Registered");
    ```

3. Create an `Activity`. Optionally, set tags:

    ```csharp
            using (var activity = RegisteredActivity.StartActivity("Main"))
            {
                activity?.SetTag("foo", "bar1");
                // your logic for Main activity
            }
    ```

4. Register your `ActivitySource` in OpenTelemetry.AutoInstrumentation
by setting the `OTEL_DOTNET_AUTO_ADDITIONAL_SOURCES` environmental variable.
You can set the value to either `Examples.ManualInstrumentations.Registerd`
or to `Examples.ManualInstrumentations.*`, which registers the entire prefix.

You can see a sample console application with automatic (`HttpClient`) and manual
instrumentation [here](..\examples\ManualInstrumenation\Examples.ManualInstrumentations).

>  Note that an `Activity` created for `NonRegistered.ManualInstrumentations`
`ActivitySoruce` is not handled by the OpenTelemetry Automatic Instrumentation.

Further reading:

- [OpenTelemetry.io documentation for .NET Manual Instrumentation](https://opentelemetry.io/docs/instrumentation/net/manual/)
