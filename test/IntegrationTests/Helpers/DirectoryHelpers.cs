// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace IntegrationTests.Helpers;

internal static class DirectoryHelpers
{
    public static DirectoryInfo CreateTempDirectory()
    {
#if NET
        return Directory.CreateTempSubdirectory("native_logs");
#else
        var tempDir = Path.Combine(Path.GetTempPath(), "native_logs_" + Path.GetRandomFileName());
        return Directory.CreateDirectory(tempDir);
#endif
    }
}
