// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace Examples.AspNetCoreMvc.Shared;

internal static class StackTraceHelper
{
    public static string[] GetUsefulStack()
    {
        var stackTrace = Environment.StackTrace;
        var methods = stackTrace.Split([" at "], StringSplitOptions.None);
        return methods;
    }
}
