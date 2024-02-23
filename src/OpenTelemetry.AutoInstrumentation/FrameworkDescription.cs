// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.Logging;

namespace OpenTelemetry.AutoInstrumentation;

internal partial class FrameworkDescription
{
    private static readonly IOtelLogger Log = OtelLogging.GetLogger();

    private FrameworkDescription(
        string name,
        string productVersion,
        string osPlatform,
        string osArchitecture,
        string processArchitecture)
    {
        Name = name;
        ProductVersion = productVersion;
        OSPlatform = osPlatform;
        OSArchitecture = osArchitecture;
        ProcessArchitecture = processArchitecture;
    }

    public string Name { get; }

    public string ProductVersion { get; }

    public string OSPlatform { get; }

    public string OSArchitecture { get; }

    public string ProcessArchitecture { get; }

    public override string ToString()
    {
        // examples:
        // .NET Framework 4.8 x86 on Windows x64
        // .NET Core 3.0.0 x64 on Linux x64
        return $"{Name} {ProductVersion} {ProcessArchitecture} on {OSPlatform} {OSArchitecture}";
    }
}
