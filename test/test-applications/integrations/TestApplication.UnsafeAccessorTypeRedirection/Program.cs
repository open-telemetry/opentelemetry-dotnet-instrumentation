// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Reflection;
using System.Runtime.CompilerServices;
using TestApplication.Shared;

var expectedAssemblyName = ArgumentHelper.GetRequiredArgument(args, "--assembly-name");
var expectedVersion = ArgumentHelper.GetArgument(args, "--expected-version", string.Empty);
var expectedTypeName = $"Some.Type, {expectedAssemblyName}";
if (!string.IsNullOrEmpty(expectedVersion))
{
    expectedTypeName += $", Version={expectedVersion}";
}

var method = typeof(UnsafeAccessorTarget).GetMethod(
    nameof(UnsafeAccessorTarget.Unused),
    BindingFlags.NonPublic | BindingFlags.Static) ??
    throw new MissingMethodException(typeof(UnsafeAccessorTarget).FullName, nameof(UnsafeAccessorTarget.Unused));

var returnTypeName = method.ReturnParameter.GetCustomAttribute<UnsafeAccessorTypeAttribute>()?.TypeName ??
    throw new InvalidOperationException(
        $"UnsafeAccessorTypeAttribute is missing from the return value of {nameof(UnsafeAccessorTarget.Unused)}.");
var parameterTypeName = method.GetParameters().Single().GetCustomAttribute<UnsafeAccessorTypeAttribute>()?.TypeName ??
    throw new InvalidOperationException(
        $"UnsafeAccessorTypeAttribute is missing from the parameter of {nameof(UnsafeAccessorTarget.Unused)}.");

if (returnTypeName != expectedTypeName || parameterTypeName != expectedTypeName)
{
    throw new InvalidOperationException(
        $"Expected UnsafeAccessorType metadata '{expectedTypeName}', " +
        $"but found return '{returnTypeName}' and parameter '{parameterTypeName}'.");
}

return 0;
