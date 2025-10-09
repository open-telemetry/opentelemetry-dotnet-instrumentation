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

no_code/development:
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
      span:
        name: Span-TestMethod1String
        kind: internal
        attributes:
          - name: custom.attribute
            value: "attribute_value"
            type: string
```

### Target Configuration

Each target specifies a method to instrument:

- **`assembly.name`**: The name of the assembly containing the target method
- **`type`**: The fully qualified type name containing the method
- **`method`**: The method name to instrument
- **`signature`**: Method signature specification for precise targeting
  - **`return_type`**: The method's return type (e.g., `System.Void`,
    `System.String`, `System.Threading.Tasks.Task`)
  - **`parameter_types`**: Array of parameter types (empty array for
    parameterless methods)

### Span Configuration

Configure the telemetry span created for the instrumented method:

- **`name`**: Custom span name (if not specified, defaults to method name)
- **`kind`**: (Optional) Span kind - one of: `internal`, `server`, `client`,
  `producer`, `consumer`. Defaults to `internal` if omitted or if an invalid
  value is provided
- **`attributes`**: Array of custom attributes to add to the span

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

```yaml
no_code/development:
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

```yaml
no_code/development:
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

```yaml
no_code/development:
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

```yaml
no_code/development:
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

```yaml
no_code/development:
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

```yaml
no_code/development:
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

```yaml
no_code/development:
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

```yaml
no_code/development:
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

```yaml
no_code/development:
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

## Span Kinds

Choose appropriate span kinds based on the method's role. The `kind` field is
optional and defaults to `internal` if omitted or if an invalid value is
provided:

- **`internal`**: Internal operations within your application (default)
- **`server`**: Methods that handle incoming requests
- **`client`**: Methods that make outgoing calls to external services
- **`producer`**: Methods that produce messages or events
- **`consumer`**: Methods that consume messages or events

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
