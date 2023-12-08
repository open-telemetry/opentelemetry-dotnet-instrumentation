// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace IntegrationTests.Helpers;

internal static class DirectoryHelpers
{
    public static DirectoryInfo CreateTempDirectory()
    {
#if NET7_0_OR_GREATER
        return Directory.CreateTempSubdirectory("native_logs");
#else
        var tempDir = Path.Combine(Path.GetTempPath(), "native_logs_" + Path.GetRandomFileName());
        return Directory.CreateDirectory(tempDir);
#endif
    }
}
