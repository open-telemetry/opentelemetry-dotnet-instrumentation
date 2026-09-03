// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace TestApplication.NoCode;

internal static class GenericNestedTypeTargets<T>
{
    public static void Invoke()
    {
        new ReferenceType().Target();
        var valueType = default(ValueType);
        valueType.Target();
    }

    private struct ValueType
    {
        private int _invocationCount;

        public void Target()
        {
            _invocationCount++;
        }
    }

    private sealed class ReferenceType
    {
        private int _invocationCount;

        public void Target()
        {
            _invocationCount++;
        }
    }
}
