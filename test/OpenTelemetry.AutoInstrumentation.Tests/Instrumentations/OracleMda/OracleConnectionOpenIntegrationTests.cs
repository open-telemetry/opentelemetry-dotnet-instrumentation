// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Reflection;
using System.Runtime.ExceptionServices;
using OpenTelemetry.AutoInstrumentation.CallTarget;
using OpenTelemetry.AutoInstrumentation.Instrumentations;
using OpenTelemetry.AutoInstrumentation.Instrumentations.OracleMda;

namespace OpenTelemetry.AutoInstrumentation.Tests.Instrumentations.OracleMda;

public class OracleConnectionOpenIntegrationTests
{
    [Theory]
    [InlineData("Open")]
    [InlineData("OpenAsync")]
    public void BeginMethodEnablesDatabaseOpenTelemetryTracing(string methodName)
    {
        var connection = new FakeOracleConnection();

        InvokeBeginMethod(methodName, connection);

        Assert.True(connection.DatabaseOpenTelemetryTracing);
    }

    private static void InvokeBeginMethod(string methodName, FakeOracleConnection connection)
    {
        // Resolve the integration type and argument types from the same InstrumentMethod
        // metadata used to generate the native CallTarget definitions.
        var (integrationType, attribute) = typeof(OracleMdaConstants).Assembly
            .GetTypes()
            .SelectMany(type => type.GetCustomAttributes<InstrumentMethodAttribute>(inherit: false).Select(attribute => (Type: type, Attribute: attribute)))
            .Single(x => x.Attribute.TypeName == OracleMdaConstants.OracleConnectionTypeName && x.Attribute.MethodName == methodName);

        var argumentTypes = attribute.ParameterTypeNames
            .Select(typeName => Type.GetType(typeName, throwOnError: true)!)
            .ToArray();

        var beginMethod = typeof(CallTargetInvoker)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(method =>
                method.Name == nameof(CallTargetInvoker.BeginMethod) &&
                method.GetGenericArguments().Length == argumentTypes.Length + 2 &&
                method.GetParameters().Length == argumentTypes.Length + 1)
            .MakeGenericMethod([integrationType, typeof(FakeOracleConnection), .. argumentTypes]);

        object?[] arguments = [connection, .. argumentTypes.Select(Activator.CreateInstance)];

        try
        {
            beginMethod.Invoke(null, arguments);
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
        }
    }

    internal sealed class FakeOracleConnection
    {
        public bool DatabaseOpenTelemetryTracing { get; set; }
    }
}
