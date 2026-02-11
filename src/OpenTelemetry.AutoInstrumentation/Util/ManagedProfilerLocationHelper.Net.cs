// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.Logging;

namespace OpenTelemetry.AutoInstrumentation.Util;

internal static partial class ManagedProfilerLocationHelper
{
    public static string ManagedProfilerRuntimeDirectory { get; } = Path.Combine(TracerHomeDirectory, "net");

    public static string ResolveManagedProfilerVersionDirectory(IOtelLogger logger)
    {
        var commonLanguageRuntimeVersionFolder = $"net{Environment.Version.Major}.{Environment.Version.Minor}";

        return Path.Combine(ManagedProfilerRuntimeDirectory, commonLanguageRuntimeVersionFolder);
    }
}
