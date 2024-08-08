// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.AutoInstrumentation.Tests.Util;

internal static class DirectoryHelpers
{
    public static DirectoryInfo CreateTempDirectory()
    {
#if NET7_0_OR_GREATER
        return Directory.CreateTempSubdirectory("managed_logs");
#else
        var tempDir = Path.Combine(Path.GetTempPath(), "managed_logs_" + Path.GetRandomFileName());
        return Directory.CreateDirectory(tempDir);
#endif
    }
}
