// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace Examples.AspNetCoreMvc.Shared;

public static class StackTraceHelper
{
    public static string[] GetUsefulStack()
    {
        var stackTrace = Environment.StackTrace;
        string[] methods = stackTrace.Split(new[] { " at " }, StringSplitOptions.None);
        return methods;
    }
}
