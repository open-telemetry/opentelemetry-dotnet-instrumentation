# Configuration based instrumentation

> [!IMPORTANT]  
> **Status:** [Experimental](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/versioning-and-stability.md)
> This feature is experimental. Functionality and the file schema is subject to
> change.
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
                value: "attribute_value"                      # Static attribute value
                type: string                                  # Attribute type (see Attribute Types section below)
              - name: dynamic.attribute                       # Dynamic attribute name
                source: $arg1                                 # Expression to extract value from method context (see Dynamic Attributes section)
                type: string
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

### Dynamic Attributes

Dynamic attributes allow you to extract attribute values from the method context
at runtime. Use the `source` property instead of `value` to specify an expression.

#### Expression Syntax

| Expression               | Description                                |
|--------------------------|--------------------------------------------|
| `$arg1`                  | Value of the first method argument         |
| `$arg2`                  | Value of the second method argument        |
| `$arg1.PropertyName`     | Property value from the first argument     |
| `$arg1.Nested.Property`  | Nested property access                     |
| `$instance`              | The instance object (for instance methods) |
| `$instance.PropertyName` | Property of the instance object            |
| `$method`                | Method name                                |
| `$type`                  | Declaring type name                        |

> [!NOTE]  
>
> - Arguments are 1-indexed (`$arg1` to `$arg9`)
> - Property access uses reflection and only works with public properties
> - If an expression evaluates to `null`, the attribute is omitted
> - Invalid expressions or property paths are silently skipped (logged at debug
>   level)

#### Dynamic Attribute Examples

Extract method argument values:

```yaml
attributes:
  - name: order.id
    source: $arg1              # First argument value
    type: int
  - name: customer.id
    source: $arg2              # Second argument value
    type: string
```

Extract property values from arguments:

```yaml
attributes:
  - name: request.url
    source: $arg1.RequestUri.AbsoluteUri
    type: string
  - name: user.email
    source: $arg1.User.Email
    type: string
```

Extract values from the instance:

```yaml
attributes:
  - name: service.name
    source: $instance.ServiceName
    type: string
  - name: merchant.id
    source: $instance.MerchantId
    type: string
```

### Function Expressions

Function expressions allow you to transform and combine values. Use them in the
`source` property for attributes, in status rule conditions, or in the
`name_source` property for dynamic span names.

#### Supported Functions

| Function                          | Description                                | Example                           |
|-----------------------------------|--------------------------------------------|-----------------------------------|
| `concat(...)`                     | Concatenates values into a string          | `concat($arg1, "-", $arg2)`       |
| `coalesce(...)`                   | Returns the first non-null/non-empty value | `coalesce($arg1.Name, "unknown")` |
| `substring(str, start, [length])` | Extracts a substring                       | `substring($arg1, 0, 10)`         |
| `tostring(value)`                 | Converts value to string                   | `tostring($arg1.Id)`              |
| `isnull(value)`                   | Returns true if value is null/empty        | `isnull($return)`                 |
| `isnotnull(value)`                | Returns true if value is not null          | `isnotnull($return.Data)`         |
| `equals(a, b)`                    | Returns true if values are equal           | `equals($return.Status, "error")` |
| `notequals(a, b)`                 | Returns true if values are not equal       | `notequals($arg1, 0)`             |

#### Function Expression Examples

Concatenate values:

```yaml
attributes:
  - name: operation.id
    source: concat($type, ".", $method)
    type: string
  - name: order.key
    source: concat($arg1.CustomerId, "-", $arg1.OrderId)
    type: string
```

Use coalesce for defaults:

```yaml
attributes:
  - name: user.name
    source: coalesce($arg1.DisplayName, $arg1.Email, "anonymous")
    type: string
```

### Dynamic Span Names

Dynamic span names allow you to construct span names at runtime based on method
context. This is useful for creating meaningful, contextual span names that
include parameter values or other runtime information.

> [!IMPORTANT]
> Span names should be low-cardinality to avoid performance issues and storage
> overhead. Avoid including high-cardinality values like unique identifiers,
> timestamps, or user-specific data directly in span names. Use span attributes
> for high-cardinality data instead. See the [OpenTelemetry Span specification](https://github.com/open-telemetry/opentelemetry-specification/blob/v1.54.0/specification/trace/api.md#span)
> for guidance on span name best practices.

Use the `name_source` property with a function expression to specify the dynamic
span name. The `name` property is still required as a fallback if the dynamic
expression fails to evaluate.

> [!NOTE]  
> Only function expressions are supported for dynamic span names (not simple
> expressions like `$arg1`). This ensures the result is always a string.

#### Dynamic Span Name Examples

Create span names from argument values:

```yaml
span:
  name: DefaultTransaction                              # Fallback name
  name_source: concat("Transaction-", $arg1)            # Dynamic name using first argument
```

Combine multiple values:

```yaml
span:
  name: DefaultQuery                                    # Fallback name
  name_source: concat("Query.", $arg1, ".", $arg2)      # e.g., "Query.ProductionDB.users"
```

Include method context:

```yaml
span:
  name: DefaultOperation                                 # Fallback name
  name_source: concat($method, "-", $arg1.OperationType) # e.g., "ProcessOrder-Express"
```

Use with nested properties:

```yaml
span:
  name: DefaultRequest                                   # Fallback name
  name_source: concat($arg1.HttpMethod, " ", $arg1.Path) # e.g., "GET /api/users"
```

### Status Configuration

You can configure span status based on return values or other conditions using
status rules. Rules are evaluated in order, and the first matching rule sets
the status.

#### Status Rule Syntax

```yaml
span:
  name: my-span
  status:
    rules:
      - condition: <expression>    # Expression that evaluates to boolean
        code: <status_code>        # ok, error, or unset
        description: <text>        # Optional description (useful for errors)
```

#### Status Codes

- **`ok`**: The operation completed successfully
- **`error`**: The operation failed
- **`unset`**: No status set (default)

#### Status Rule Examples

Set error status when return value indicates failure:

```yaml
span:
  name: process-order
  status:
    rules:
      - condition: isnull($return)
        code: error
        description: "Order processing returned null"
      - condition: equals($return.Success, false)
        code: error
        description: "Order processing failed"
      - condition: isnotnull($return)
        code: ok
```

Set status based on return value properties:

```yaml
span:
  name: http-request
  status:
    rules:
      - condition: equals($return.StatusCode, 500)
        code: error
        description: "Internal server error"
      - condition: equals($return.StatusCode, 404)
        code: error
        description: "Not found"
      - condition: isnotnull($return)
        code: ok
```

#### Complete example

Complete example with dynamic attributes and status:

```yaml
instrumentation/development:
  dotnet:
    no_code:
      targets:
        - target:
            assembly:
              name: MyApp.Services
            type: MyApp.Services.OrderService
            method: ProcessOrder
            signature:
              return_type: MyApp.Models.OrderResult
              parameter_types:
                - MyApp.Models.OrderRequest
          span:
            name: process-order
            kind: internal
            attributes:
              - name: order.id
                source: $arg1.OrderId
                type: string
              - name: customer.id
                source: $arg1.CustomerId
                type: string
              - name: order.total
                source: $arg1.TotalAmount
                type: double
              - name: operation.name
                source: concat("ProcessOrder-", $arg1.OrderType)
                type: string
            status:
              rules:
                - condition: isnull($return)
                  code: error
                  description: "Null result returned"
                - condition: equals($return.Status, "Failed")
                  code: error
                  description: "Order processing failed"
                - condition: equals($return.Status, "Completed")
                  code: ok
```

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

### Generic Class Instrumentation

Instrument methods in generic classes with class-level type parameters:

```csharp
public class GenericNoCodeTestingClass<TFooClass, TBarClass>
{
    public TFooMethod GenericTestMethod<TFooMethod, TBarMethod>(
        TFooMethod fooMethod, 
        TBarMethod barMethod, 
        TFooClass fooClass, 
        TBarClass barClass)
    {
        return fooMethod;
    }
}
```

For generic classes, use the backtick notation with the number of class-level
type parameters:

Generic Type Parameter Notation:

- Class-level type parameters: Use `!!0`, `!!1`, etc. (where `!!0` is the first
  class type parameter)
- Method-level type parameters: Use `!0`, `!1`, etc. (where `!0` is the first
  method type parameter)
- In the type name, use backtick notation: `ClassName\`N` where N is the number
  of generic parameters

Configuration:

```yaml
instrumentation/development:
  dotnet:
    no_code:
      targets:
        - target:
            assembly:
              name: TestApplication.NoCode
            type: TestApplication.NoCode.GenericNoCodeTestingClass`2
            method: GenericTestMethod
            signature:
              return_type: '!0'
              parameter_types:
                - '!0'
                - '!1'
                - '!!0'
                - '!!1'
          span:
            name: Span-GenericTestMethodWithParameters
            kind: internal
```

In this example:

- `GenericNoCodeTestingClass\`2` indicates a class with 2 generic type parameters
- `'!0'` represents the first method type parameter (`TFooMethod`)
- `'!1'` represents the second method type parameter (`TBarMethod`)
- `'!!0'` represents the first class type parameter (`TFooClass`)
- `'!!1'` represents the second class type parameter (`TBarClass`)

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
