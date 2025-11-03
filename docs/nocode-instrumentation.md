# Configuration based instrumentation

> [!IMPORTANT]  
> **Status:** [Experimental](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/versioning-and-stability.md)
> This feature is experimental. Functionality and the file schema is subject to change.
>
> To utilize this feature you need to switch to [file-based configuration](file-based-configuration.md)

## Overview

Configuration-based instrumentation (also known as "no-code instrumentation")
allows you to instrument methods in any .NET library without modifying the
source code. This feature enables automatic span creation and telemetry
collection for specified methods through YAML configuration.

> [!NOTE]  
> **Performance Considerations:** No-code instrumentation uses bytecode
> manipulation techniques which may impact application performance. For optimal
> performance, consider using [manual instrumentation](manual-instrumentation.md)
> when you can modify the source code directly.

## Key Features

- **Universal Method Instrumentation**: Instrument methods in any .NET assembly
  without code changes
- **Flexible Method Targeting**: Support for static methods, instance methods,
  async methods, and generic methods
- **Parameter Support**: Handle methods with up to 9 parameters
- **Return Type Support**: Compatible with various return types including
  `void`, primitives, custom classes, `Task`, `Task<T>`, `ValueTask`, and
  `ValueTask<T>`
- **Span Customization**: Configure span names, kinds, and custom attributes
- **Method Overload Support**: Target specific method overloads using precise
  signature matching

## Limitations

- **Parameter Limit**: Methods with more than 9 parameters are not supported
- **Experimental Status**: The feature and configuration schema may change

## Configuration Structure

The no-code instrumentation configuration is defined under the
`no_code/development` section in your YAML configuration file:

```yaml
file_format: "1.0-rc.1"

instrumentation/development:
  dotnet:
    no_code:
      targets:
        - target:                                             # Method target specification
            assembly:                                         # Assembly information
              name: TestApplication.NoCode                    # The name of the assembly containing the target method
            type: TestApplication.NoCode.NoCodeTestingClass   # The fully qualified type name containing the method
            method: TestMethod                                # The method name to instrument
            signature:                                        # Method signature specification for precise targeting
              return_type: System.Void                        # The method's return type (e.g., System.Void, System.String, System.Threading.Tasks.Task)
              parameter_types:                                # Array of parameter types (empty array for parameterless methods)
                - System.String
          span:
            name: Span-TestMethod1String                      # Custom span name (if not specified, defaults to method name)
            kind: internal                                    # (Optional) Span kind: internal, server, client, producer, consumer (defaults to internal)
            attributes:                                       # Array of custom attributes to add to the span
              - name: custom.attribute                        # Attribute name
                value: "attribute_value"                      # Attribute value
                type: string                                  # Attribute type (see Attribute Types section below)
```

### Attribute Types

Supported attribute types and their formats:

- **`string`**: Text values
- **`bool`**: Boolean values (`true`/`false`)
- **`int`**: Integer values
- **`double`**: Floating-point values
- **`string_array`**: Array of strings
- **`bool_array`**: Array of booleans
- **`int_array`**: Array of integers
- **`double_array`**: Array of floating-point numbers

> [!NOTE]  
> Attributes with unknown types or invalid values will be omitted from the span.
> Ensure attribute types match the supported formats above to avoid data loss.

For more information about attributes, see the [OpenTelemetry Attribute specification](https://opentelemetry.io/docs/specs/otel/common/#attribute).

## Examples

### Basic Method Instrumentation

Instrument a simple static method:

```csharp
public static void TestMethodStatic();
```

Configuration:

```yaml
instrumentation/development:
  dotnet:
    no_code:
      targets:
        - target:
            assembly:
              name: TestApplication.NoCode
            type: TestApplication.NoCode.NoCodeTestingClass
            method: TestMethodStatic
            signature:
              return_type: System.Void
              parameter_types:
          span:
            name: Span-TestMethodStatic
            kind: internal
```

### Method Instrumentation with Default Span Kind

When `kind` is omitted, it defaults to `internal`:

```csharp
public void TestMethodA();
```

Configuration:

```yaml
instrumentation/development:
  dotnet:
    no_code:
      targets:
        - target:
            assembly:
              name: TestApplication.NoCode
            type: TestApplication.NoCode.NoCodeTestingClass
            method: TestMethodA
            signature:
              return_type: System.Void
              parameter_types:
          span:
            name: Span-TestMethodA
            # kind defaults to 'internal' when omitted
```

### Method with Parameters

Instrument a method with specific parameters:

```csharp
public void TestMethod(string param1, string param2);
```

Configuration:

```yaml
instrumentation/development:
  dotnet:
    no_code:
      targets:
        - target:
            assembly:
              name: TestApplication.NoCode
            type: TestApplication.NoCode.NoCodeTestingClass
            method: TestMethod
            signature:
              return_type: System.Void
              parameter_types:
                - System.String
                - System.String
          span:
            name: Span-TestMethod2
            kind: server
            attributes:
              - name: operation.type
                value: "test_method"
                type: string
```

### Async Method Instrumentation

Instrument an async method returning `Task<T>`:

```csharp
public async Task<int> IntTaskTestMethodAsync();
```

Configuration:

```yaml
instrumentation/development:
  dotnet:
    no_code:
      targets:
        - target:
            assembly:
              name: TestApplication.NoCode
            type: TestApplication.NoCode.NoCodeTestingClass
            method: IntTaskTestMethodAsync
            signature:
              return_type: System.Threading.Tasks.Task`1[System.Int32]
              parameter_types:
          span:
            name: Span-IntTaskTestMethodAsync
            kind: client
            attributes:
              - name: async.operation
                value: "task_with_return"
                type: string
```

### Multiple Attributes with Different Types

Configure spans with various attribute types (from the actual test configuration):

```csharp
public static void TestMethodStatic();
```

Configuration with multiple attribute types:

```yaml
instrumentation/development:
  dotnet:
    no_code:
      targets:
        - target:
            assembly:
              name: TestApplication.NoCode
            type: TestApplication.NoCode.NoCodeTestingClass
            method: TestMethodStatic
            signature:
              return_type: System.Void
              parameter_types:
          span:
            name: Span-TestMethodStatic
            kind: internal
            attributes:
              - name: attribute_key_string
                value: "string_value"
                type: string
              - name: attribute_key_bool
                value: true
                type: bool
              - name: attribute_key_int
                value: 12345
                type: int
              - name: attribute_key_double
                value: 123.45
                type: double
              - name: attribute_key_string_array
                value: ["value1", "value2", "value3"]
                type: string_array
              - name: attribute_key_bool_array
                value: [true, false, true]
                type: bool_array
              - name: attribute_key_int_array
                value: [123, 456, 789]
                type: int_array
              - name: attribute_key_double_array
                value: [123.45, 678.90]
                type: double_array
```

### Method Overload Targeting

Target specific method overloads by parameter types:

```csharp
// Parameterless overload
public void TestMethod();

// String parameter overload
public void TestMethod(string param1);

// Int parameter overload
public void TestMethod(int param1);
```

Configuration for targeting specific overloads:

```yaml
instrumentation/development:
  dotnet:
    no_code:
      targets:
        # Parameterless overload
        - target:
            assembly:
              name: TestApplication.NoCode
            type: TestApplication.NoCode.NoCodeTestingClass
            method: TestMethod
            signature:
              return_type: System.Void
              parameter_types:
          span:
            name: Span-TestMethod0
            kind: client
        
        # String parameter overload
        - target:
            assembly:
              name: TestApplication.NoCode
            type: TestApplication.NoCode.NoCodeTestingClass
            method: TestMethod
            signature:
              return_type: System.Void
              parameter_types:
                - System.String
          span:
            name: Span-TestMethod1String
            kind: producer
        
        # Int parameter overload
        - target:
            assembly:
              name: TestApplication.NoCode
            type: TestApplication.NoCode.NoCodeTestingClass
            method: TestMethod
            signature:
              return_type: System.Void
              parameter_types:
                - System.Int32
          span:
            name: Span-TestMethod1Int
            kind: server
```

### ValueTask Support (.NET 8+ only)

Instrument methods returning `ValueTask` or `ValueTask<T>`:

```csharp
public async ValueTask<int> IntValueTaskTestMethodAsync();
```

Configuration:

```yaml
instrumentation/development:
  dotnet:
    no_code:
      targets:
        - target:
            assembly:
              name: TestApplication.NoCode
            type: TestApplication.NoCode.NoCodeTestingClass
            method: IntValueTaskTestMethodAsync
            signature:
              return_type: System.Threading.Tasks.ValueTask`1[System.Int32]
              parameter_types:
          span:
            name: Span-IntValueTaskTestMethodAsync
            kind: client
```

### Generic Method Instrumentation

Instrument generic methods (note the return type specification):

```csharp
public T? GenericTestMethod<T>();
```

> [!TIP]  
> **Finding Generic Method Types**: For generic methods, you need to specify
> the concrete types after generic type substitution for both return types and
> parameter types. Use tools like [ILSpy](https://github.com/icsharpcode/ILSpy)
> or similar IL viewers to examine the compiled method signatures and determine
> the exact types to use in your configuration. In IlSpy you can use IL with C#
> in drop down list.

Configuration (when called as `GenericTestMethod<int>()`):

```yaml
instrumentation/development:
  dotnet:
    no_code:
      targets:
        - target:
            assembly:
              name: TestApplication.NoCode
            type: TestApplication.NoCode.NoCodeTestingClass
            method: GenericTestMethod
            signature:
              return_type: System.Int32
              parameter_types:
          span:
            name: Span-GenericTestMethod
            kind: internal
```

### Methods with Return Values

Instrument methods that return values:

```csharp
// Method returning string
public string ReturningStringTestMethod();

// Method returning custom class
public TestClass ReturningCustomClassTestMethod();
```

Configuration:

```yaml
instrumentation/development:
  dotnet:
    no_code:
      targets:
        # Method returning string
        - target:
            assembly:
              name: TestApplication.NoCode
            type: TestApplication.NoCode.NoCodeTestingClass
            method: ReturningStringTestMethod
            signature:
              return_type: System.String
              parameter_types:
          span:
            name: Span-ReturningStringTestMethod
            kind: internal
        
        # Method returning custom class
        - target:
            assembly:
              name: TestApplication.NoCode
            type: TestApplication.NoCode.NoCodeTestingClass
            method: ReturningCustomClassTestMethod
            signature:
              return_type: TestApplication.NoCode.TestClass
              parameter_types:
          span:
            name: Span-ReturningCustomClassTestMethod
            kind: internal
```

## Common Return Types

When specifying return types in the signature, use these common .NET type names:

- **Void methods**: `System.Void`
- **Basic types**: `System.String`, `System.Int32`, `System.Boolean`, `System.Double`
- **Task methods**: `System.Threading.Tasks.Task`
- **Task with return**: `System.Threading.Tasks.Task\`1[ReturnType]`
- **ValueTask methods**: `System.Threading.Tasks.ValueTask`
- **ValueTask with return**: `System.Threading.Tasks.ValueTask\`1[ReturnType]`
- **Custom classes**: `YourNamespace.YourClassName`

## Test Application Reference

For complete working examples, see the test application:

- **Project**: [`test/test-applications/integrations/TestApplication.NoCode/TestApplication.NoCode.csproj`](../test/test-applications/integrations/TestApplication.NoCode/TestApplication.NoCode.csproj)
- **Configuration**: [`test/test-applications/integrations/TestApplication.NoCode/config.yaml`](../test/test-applications/integrations/TestApplication.NoCode/config.yaml)
- **Test Classes**: [`test/test-applications/integrations/TestApplication.NoCode/NoCodeTestingClass.cs`](../test/test-applications/integrations/TestApplication.NoCode/NoCodeTestingClass.cs)

The test application demonstrates instrumentation of:

- Static and instance methods
- Methods with 0-9 parameters
- Async methods (`Task`, `Task<T>`, `ValueTask`, `ValueTask<T>`)
- Methods with different return types
- Generic methods
- Method overloads
- Custom span attributes with all supported types

## Best Practices

1. **Consider Manual Instrumentation First**: If you can modify the source code,
   prefer [manual instrumentation](manual-instrumentation.md) for better
   performance and more control over telemetry
2. **Use Descriptive Span Names**: Choose clear, consistent span names that
   describe the operation
3. **Set Appropriate Span Kinds**: Select span kinds that accurately represent
   the method's role
4. **Add Meaningful Attributes**: Include attributes that provide valuable
   context for observability
5. **Target Specific Overloads**: Use precise signatures to instrument the
   exact method overload you need
6. **Test Configuration**: Validate your configuration with a test application
   before deploying
7. **Monitor Performance**: Be mindful of performance impact when instrumenting
   high-frequency methods. Consider the cost-benefit of instrumenting each
   method

## Troubleshooting

- **Method Not Instrumented**: Verify the assembly name, type name, method
  name, and signature match exactly
- **Wrong Method Targeted**: Check parameter types in the signature - they must
  match the target method precisely
- **Generic Methods**: Ensure the return type specification is correct for
  generic methods
- **Performance Issues**: Consider the frequency of method calls and whether
  instrumentation is necessary for high-throughput methods
- **Debug Logs**: Enable debug logging (see [Global settings](config.md#global-settings))
  and search for log messages prefixed with `No code` to find information
  specific to no-code instrumentation, including configuration parsing, method
  targeting, and instrumentation application details.
