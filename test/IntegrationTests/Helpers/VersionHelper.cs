// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Reflection;

namespace IntegrationTests.Helpers;

internal static class VersionHelper
{
    public static string AutoInstrumentationVersion
    {
        get
        {
            return typeof(OpenTelemetry.AutoInstrumentation.Constants).Assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion.Split('+')[0];
        }
    }

    public static string SdkVersion
    {
        get
        {
            return typeof(OpenTelemetry.Resources.Resource).Assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion.Split('+')[0];
        }
    }
}
