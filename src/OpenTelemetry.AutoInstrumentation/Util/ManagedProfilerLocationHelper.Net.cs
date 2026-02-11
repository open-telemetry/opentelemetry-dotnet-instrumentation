// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.Logging;

namespace OpenTelemetry.AutoInstrumentation.Util;

internal static partial class ManagedProfilerLocationHelper
{
    public static string ResolveManagedProfilerDirectory(IOtelLogger logger)
    {
        var frameworkDirectoryName = "net";
        var commonLanguageRuntimeVersionFolder = $"net{Environment.Version.Major}.{Environment.Version.Minor}";

        return Path.Combine(TracerHomeDirectory, frameworkDirectoryName, commonLanguageRuntimeVersionFolder);
    }
}
