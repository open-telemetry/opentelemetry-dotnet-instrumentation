// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Reflection;

namespace IntegrationTests.Helpers;

internal static class OtlpResourceExpectorExtensions
{
    public static void ExpectStandardResources(this OtlpResourceExpector resourceExpector, int processId, string serviceName)
    {
        resourceExpector.Expect("service.name", serviceName); // this is set via env var and App.config, but env var has precedence
#if NETFRAMEWORK
        resourceExpector.Expect("deployment.environment.name", "test"); // this is set via App.config
#endif
        resourceExpector.Expect("telemetry.sdk.name", "opentelemetry");
        resourceExpector.Expect("telemetry.sdk.language", "dotnet");
        resourceExpector.Expect("telemetry.sdk.version", typeof(OpenTelemetry.Resources.Resource).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion.Split('+')[0]);
        resourceExpector.Expect("telemetry.distro.name", "opentelemetry-dotnet-instrumentation");
        resourceExpector.Expect("telemetry.distro.version", typeof(OpenTelemetry.AutoInstrumentation.Constants).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion.Split('+')[0]);

        resourceExpector.Expect("process.pid", processId);
        resourceExpector.Expect("host.name", Environment.MachineName);

#if NETFRAMEWORK
        resourceExpector.Expect("process.runtime.name", ".NET Framework");
#else
        resourceExpector.Expect("process.runtime.name", ".NET");
#endif

        var expectedPlatform = EnvironmentTools.GetOS() switch
        {
            "win" => "windows",
            "osx" => "darwin",
            "linux" => "linux",
            _ => throw new PlatformNotSupportedException($"Unknown platform")
        };
        resourceExpector.Expect("os.type", expectedPlatform);
        resourceExpector.Exist("os.build_id");
        resourceExpector.Exist("os.description");
        resourceExpector.Exist("os.name");
        resourceExpector.Exist("os.version");
    }
}
