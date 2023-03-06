// <copyright file="TempDirectory.cs" company="OpenTelemetry Authors">
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

namespace DependencyListGenerator.DotNetOutdated;

internal class TempDirectory : IDisposable
{
    private string tempPath;
    private string tempDirName;

    public TempDirectory()
    {
        tempPath = Path.GetTempPath();
        tempDirName = Path.GetRandomFileName();
        Directory.CreateDirectory(DirectoryPath);
    }

    public string DirectoryPath
    {
        get => Path.Combine(tempPath, tempDirName);
    }

    public void Dispose()
    {
        Directory.Delete(DirectoryPath, true);
    }
}
