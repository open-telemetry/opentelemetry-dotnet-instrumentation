// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Reflection;
using System.Runtime.CompilerServices;
using TestApplication.Shared;

var expectedAssemblyName = ArgumentHelper.GetRequiredArgument(args, "--assembly-name");
var expectedVersion = ArgumentHelper.GetArgument(args, "--expected-version", string.Empty);
const string serviceProviderOptionsTypeName = "Microsoft.Extensions.DependencyInjection.ServiceProviderOptions";
var expectedTypeName = $"{serviceProviderOptionsTypeName}, {expectedAssemblyName}";
if (!string.IsNullOrEmpty(expectedVersion))
{
    expectedTypeName += $", Version={expectedVersion}";
}

var newServiceProviderOptionsMethod = typeof(UnsafeAccessorTarget).GetMethod(
    nameof(UnsafeAccessorTarget.NewServiceProviderOptions),
    BindingFlags.NonPublic | BindingFlags.Static) ??
    throw new MissingMethodException(typeof(UnsafeAccessorTarget).FullName, nameof(UnsafeAccessorTarget.NewServiceProviderOptions));
var returnTypeName = newServiceProviderOptionsMethod.ReturnParameter.GetCustomAttribute<UnsafeAccessorTypeAttribute>()?.TypeName ??
    throw new InvalidOperationException(
        $"UnsafeAccessorTypeAttribute is missing from the return value of {nameof(UnsafeAccessorTarget.NewServiceProviderOptions)}.");

var getValidateScopesMethod = typeof(UnsafeAccessorTarget).GetMethod(
    nameof(UnsafeAccessorTarget.GetValidateScopes),
    BindingFlags.NonPublic | BindingFlags.Static) ??
    throw new MissingMethodException(typeof(UnsafeAccessorTarget).FullName, nameof(UnsafeAccessorTarget.GetValidateScopes));
var parameterTypeName = getValidateScopesMethod.GetParameters().Single().GetCustomAttribute<UnsafeAccessorTypeAttribute>()?.TypeName ??
    throw new InvalidOperationException(
        $"UnsafeAccessorTypeAttribute is missing from the parameter of {nameof(UnsafeAccessorTarget.GetValidateScopes)}.");

// Check both attribute values before invocation: a compatible assembly could otherwise hide a missed metadata rewrite.
if (returnTypeName != expectedTypeName || parameterTypeName != expectedTypeName)
{
    throw new InvalidOperationException(
        $"Expected UnsafeAccessorType metadata '{expectedTypeName}', " +
        $"but found return '{returnTypeName}' and parameter '{parameterTypeName}'.");
}

// Without UnsafeAccessorAttribute, the extern declarations have no JIT implementation and cannot be invoked.
#if COMPLETE_UNSAFE_ACCESSOR
// The constructor tests the return-value annotation and must produce the expected concrete type and assembly.
var options = UnsafeAccessorTarget.NewServiceProviderOptions();
var optionsType = options.GetType();
var loadedAssembly = optionsType.Assembly.GetName();
if (optionsType.FullName != serviceProviderOptionsTypeName ||
    loadedAssembly.Name != expectedAssemblyName ||
    loadedAssembly.Version?.ToString() != expectedVersion)
{
    throw new InvalidOperationException(
        $"Expected {serviceProviderOptionsTypeName} from {expectedAssemblyName}, Version={expectedVersion}, " +
        $"but got {optionsType.FullName} from {loadedAssembly}.");
}

// The getter tests the parameter annotation by invoking a member on the constructed object.
if (UnsafeAccessorTarget.GetValidateScopes(options))
{
    throw new InvalidOperationException("Expected a new ServiceProviderOptions to have ValidateScopes disabled.");
}
#endif

return 0;
