// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Reflection;
using System.Runtime.CompilerServices;
using TestApplication.Shared;

var expectedAssemblyName = ArgumentHelper.GetRequiredArgument(args, "--assembly-name");
var expectedVersion = ArgumentHelper.GetArgument(args, "--expected-version", string.Empty);
const string serviceCollectionTypeName = "Microsoft.Extensions.DependencyInjection.ServiceCollection";
var expectedTypeName = $"{serviceCollectionTypeName}, {expectedAssemblyName}";
if (!string.IsNullOrEmpty(expectedVersion))
{
    expectedTypeName += $", Version={expectedVersion}";
}

var newServiceCollectionMethod = typeof(UnsafeAccessorTarget).GetMethod(
    nameof(UnsafeAccessorTarget.NewServiceCollection),
    BindingFlags.NonPublic | BindingFlags.Static) ??
    throw new MissingMethodException(typeof(UnsafeAccessorTarget).FullName, nameof(UnsafeAccessorTarget.NewServiceCollection));
var returnTypeName = newServiceCollectionMethod.ReturnParameter.GetCustomAttribute<UnsafeAccessorTypeAttribute>()?.TypeName ??
    throw new InvalidOperationException(
        $"UnsafeAccessorTypeAttribute is missing from the return value of {nameof(UnsafeAccessorTarget.NewServiceCollection)}.");

var getCountMethod = typeof(UnsafeAccessorTarget).GetMethod(
    nameof(UnsafeAccessorTarget.GetCount),
    BindingFlags.NonPublic | BindingFlags.Static) ??
    throw new MissingMethodException(typeof(UnsafeAccessorTarget).FullName, nameof(UnsafeAccessorTarget.GetCount));
var parameterTypeName = getCountMethod.GetParameters().Single().GetCustomAttribute<UnsafeAccessorTypeAttribute>()?.TypeName ??
    throw new InvalidOperationException(
        $"UnsafeAccessorTypeAttribute is missing from the parameter of {nameof(UnsafeAccessorTarget.GetCount)}.");

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
var collection = UnsafeAccessorTarget.NewServiceCollection();
var collectionType = collection.GetType();
var loadedAssembly = collectionType.Assembly.GetName();
if (collectionType.FullName != serviceCollectionTypeName ||
    loadedAssembly.Name != expectedAssemblyName ||
    loadedAssembly.Version?.ToString() != expectedVersion)
{
    throw new InvalidOperationException(
        $"Expected {serviceCollectionTypeName} from {expectedAssemblyName}, Version={expectedVersion}, " +
        $"but got {collectionType.FullName} from {loadedAssembly}.");
}

// The getter tests the parameter annotation by invoking a member on the constructed object.
if (UnsafeAccessorTarget.GetCount(collection) != 0)
{
    throw new InvalidOperationException("Expected a new ServiceCollection to be empty.");
}
#endif

return 0;
