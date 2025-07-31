# OpenTelemetry.AutoInstrumentation.Sdk.Configuration.Tests

This project contains unit tests for the `OpenTelemetry.AutoInstrumentation.Sdk.Configuration` library.

## Test Coverage

The test project focuses on testing the public APIs of the configuration library:

### OpenTelemetryServiceCollectionExtensionsTests
Tests for the `ConfigureOpenTelemetry` extension methods:
- Validates service registration with default and custom configuration section names
- Verifies parameter validation (null, empty, whitespace handling)
- Confirms options factory and monitor registration
- Tests multiple registration scenarios

### OpenTelemetryOptionsTests  
Tests for the main `OpenTelemetryOptions` class:
- Validates that all public properties are read-only
- Confirms expected public properties exist with correct types
- Verifies the structure and accessibility of the options object

## Design Considerations

The tests are designed to:
- **Focus on Public APIs**: Only test publicly accessible members to avoid tight coupling with internal implementation details
- **Use Dependency Injection**: Test through the Microsoft DI container to ensure proper integration
- **Validate Behavior**: Focus on testing expected behavior rather than implementation details
- **Maintain Flexibility**: Allow internal refactoring without breaking tests

## Test Approach

Since most of the configuration parsing logic is internal, the tests validate:
1. **Service Registration**: Ensuring the DI container setup works correctly
2. **Public API Contracts**: Verifying public properties and methods behave as expected  
3. **Parameter Validation**: Testing edge cases and error conditions
4. **Integration Points**: Confirming the library integrates properly with Microsoft.Extensions frameworks

## Running Tests

```bash
# Run all tests in the project
dotnet test

# Run with verbose output
dotnet test --verbosity normal

# Run specific test class
dotnet test --filter "FullyQualifiedName~OpenTelemetryServiceCollectionExtensionsTests"
```

## Future Enhancements

When internal APIs become public or new public APIs are added, additional test coverage can be added for:
- Configuration parsing logic  
- Factory implementations
- Options validation
- Resource attribute resolution
