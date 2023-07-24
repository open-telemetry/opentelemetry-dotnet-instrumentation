// <copyright file="TestAppStartupMode.cs" company="OpenTelemetry Authors">
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

public enum TestAppStartupMode
{
    /// <summary>
    /// Automatically determine startup mode
    /// Dotnet Core = DotnetCLI
    /// Dotnet FX = Exe
    /// </summary>
    Auto,

    /// <summary>
    /// Execute using Dotnet CLI (e.g.: dotnet.exe MyApp.dll)
    /// </summary>
    DotnetCLI,

    /// <summary>
    /// Execute directly using compiled exe (e.g.: MyApp.exe)
    /// </summary>
    Exe
}
