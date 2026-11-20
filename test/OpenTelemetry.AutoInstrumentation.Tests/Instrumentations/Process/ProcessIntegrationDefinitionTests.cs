// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using System.Reflection;
using OpenTelemetry.AutoInstrumentation.CallTarget.Handlers;
using OpenTelemetry.AutoInstrumentation.Instrumentations;
using OpenTelemetry.AutoInstrumentation.Instrumentations.Process.Integrations;

namespace OpenTelemetry.AutoInstrumentation.Tests.Instrumentations.Process;

public class ProcessIntegrationDefinitionTests
{
    [Theory]
    [InlineData(typeof(ActivitySource))]
    [InlineData(typeof(System.Diagnostics.Process))]
    public void DefinitionAndHandlerMatchCurrentRuntime(Type targetType)
    {
#if NET
        ArgumentNullException.ThrowIfNull(targetType);
#else
        if (targetType is null)
        {
            throw new ArgumentNullException(nameof(targetType));
        }
#endif
        var assemblyName = targetType.Assembly.GetName();
        var version = assemblyName.Version!;
        var targetVersion = new Version(version.Major, version.Minor, version.Build);
        var (integrationType, definition) = typeof(ProcessStartIntegration).Assembly
            .GetTypes()
            .SelectMany(type => type.GetCustomAttributes<InstrumentMethodAttribute>(inherit: false).Select(attribute => (Type: type, Attribute: attribute)))
            .Single(item => item.Attribute.IntegrationName == "Process" &&
                            item.Attribute.TypeName == targetType.FullName &&
                            item.Attribute.AssemblyName == assemblyName.Name &&
                            new Version(item.Attribute.MinimumVersion) <= targetVersion &&
                            new Version(item.Attribute.MaximumVersion) >= targetVersion);

        var method = Assert.Single(
            targetType.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance),
            method => method.Name == definition.MethodName &&
                      method.GetParameters().Select(parameter => GetTypeName(parameter.ParameterType)).SequenceEqual(definition.ParameterTypeNames));

        Assert.Equal(definition.ReturnTypeName, GetTypeName(method.ReturnType));
        Assert.NotNull(IntegrationMapper.CreateBeginMethodDelegate(
            integrationType,
            targetType,
            method.GetParameters().Select(parameter => parameter.ParameterType.MakeByRefType()).ToArray()));
    }

    private static string GetTypeName(Type type)
    {
        return type.IsGenericType
            ? $"{type.GetGenericTypeDefinition().FullName}[{string.Join(",", type.GetGenericArguments().Select(GetTypeName))}]"
            : type.FullName!;
    }
}
