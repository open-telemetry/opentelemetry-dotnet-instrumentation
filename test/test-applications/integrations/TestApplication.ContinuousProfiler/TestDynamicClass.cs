// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Dynamic;

namespace My.Custom.Test.Namespace;

internal sealed class TestDynamicClass : DynamicObject
{
    public override bool TryInvoke(InvokeBinder binder, object?[]? args, out object? result)
    {
        TestApplication.ContinuousProfiler.Vb.ClassVb.MethodVb("testParam");
        result = null;
        return true;
    }
}
