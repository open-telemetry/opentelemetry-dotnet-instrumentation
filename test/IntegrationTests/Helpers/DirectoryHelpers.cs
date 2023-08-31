// <copyright file="DirectoryHelpers.cs" company="OpenTelemetry Authors">
// Copyright The OpenTelemetry Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>

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
