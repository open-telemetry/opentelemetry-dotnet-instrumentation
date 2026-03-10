// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace IntegrationTests.Helpers;

#pragma warning disable CA1515 // Consider making public types internal. Needed for xunit tests.
public enum TestAppStartupMode
#pragma warning restore CA1515 // Consider making public types internal. Needed for xunit tests.
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
