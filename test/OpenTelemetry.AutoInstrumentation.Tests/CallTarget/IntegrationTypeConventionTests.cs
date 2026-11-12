// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Reflection;
using OpenTelemetry.AutoInstrumentation.Instrumentations;

namespace OpenTelemetry.AutoInstrumentation.Tests.CallTarget;

public class IntegrationTypeConventionTests
{
    [Fact]
    public void IntegrationTypesDeclareAtMostOneOnMethodBegin()
    {
        // IntegrationMapper resolves OnMethodBegin by name only, so overloads cause an
        // AmbiguousMatchException at runtime when the instrumented method is first called.
        var offenders = typeof(InstrumentMethodAttribute).Assembly
            .GetTypes()
            .Where(type => type.GetCustomAttributes<InstrumentMethodAttribute>(inherit: false).Any())
            .Where(type => type
                .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                .Count(method => method.Name == "OnMethodBegin") > 1)
            .Select(type => type.FullName)
            .ToList();

        Assert.Empty(offenders);
    }
}
