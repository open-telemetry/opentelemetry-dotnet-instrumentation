// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NET || NETCOREAPP
using System.Runtime.InteropServices;

using static OpenTelemetry.AutoInstrumentation.Constants.EnvironmentVariables;

namespace OpenTelemetry.AutoInstrumentation.Util;

/// <summary>
/// Probes native attachment without initializing logging or SDK dependencies before assembly isolation.
/// </summary>
internal static class NativeProfilerHelper
{
    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private delegate bool IsProfilerAttachedDelegate();

    public static bool IsOpenTelemetryProfiler(string? profilerId)
    {
        return string.Equals(profilerId, ProfilerId, StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsProfilerAttached()
    {
        try
        {
            if (IsOpenTelemetryProfiler(Environment.GetEnvironmentVariable(ProfilerIdVariable)))
            {
                var profilerPath = GetProfilerPath(ProfilerPathVariable, includeGenericPath: true);
                if (profilerPath is not null && IsProfilerAttached(profilerPath))
                {
                    return true;
                }
            }

            // CLRIE exposes the chained profiler separately from the CLR-visible profiler.
            if (IsOpenTelemetryProfiler(Environment.GetEnvironmentVariable("MicrosoftInstrumentationEngine_RawProfilerHook")))
            {
                var profilerPath = GetProfilerPath("MicrosoftInstrumentationEngine_RawProfilerHookPath", includeGenericPath: false);
                if (profilerPath is not null && IsProfilerAttached(profilerPath))
                {
                    return true;
                }
            }

            // Other proxies can load OTel from the distribution while CORECLR_PROFILER_PATH points to the proxy.
            var home = Environment.GetEnvironmentVariable(Configurations.ConfigurationKeys.TracerHome);
            var architecture = RuntimeInformation.ProcessArchitecture switch
            {
                Architecture.X86 => "x86",
                Architecture.X64 => "x64",
                Architecture.Arm64 => "arm64",
                _ => null
            };

            if (string.IsNullOrWhiteSpace(home) || architecture is null)
            {
                return false;
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return IsProfilerAttached(Path.Combine(home, $"win-{architecture}", "OpenTelemetry.AutoInstrumentation.Native.dll"));
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return IsProfilerAttached(Path.Combine(home, $"osx-{architecture}", "OpenTelemetry.AutoInstrumentation.Native.dylib"));
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                // Try both distribution layouts without loading a dependency to detect the libc implementation.
                return IsProfilerAttached(Path.Combine(home, $"linux-{architecture}", "OpenTelemetry.AutoInstrumentation.Native.so")) ||
                    IsProfilerAttached(Path.Combine(home, $"linux-musl-{architecture}", "OpenTelemetry.AutoInstrumentation.Native.so"));
            }
        }
        catch (Exception)
        {
            // This probe runs before logging and the initialization error handler are available.
            // The diagnostics rule reports a failed explicitly configured OTel deployment later.
        }

        return false;
    }

    private static string? GetProfilerPath(string variable, bool includeGenericPath)
    {
        var architectureSuffix = RuntimeInformation.ProcessArchitecture switch
        {
            Architecture.Arm64 => "_ARM64",
            Architecture.Arm => "_ARM32",
            _ => null
        };

        var path = architectureSuffix is null ? null : Environment.GetEnvironmentVariable(variable + architectureSuffix);
        if (string.IsNullOrWhiteSpace(path))
        {
            path = Environment.GetEnvironmentVariable(variable + (Environment.Is64BitProcess ? "_64" : "_32"));
        }

        if (string.IsNullOrWhiteSpace(path) && includeGenericPath)
        {
            path = Environment.GetEnvironmentVariable(variable);
        }

        return string.IsNullOrWhiteSpace(path) ? null : path;
    }

    private static bool IsProfilerAttached(string profilerPath)
    {
        var handle = (nint)0;
        try
        {
            if (NativeLibrary.TryLoad(profilerPath, out handle) && NativeLibrary.TryGetExport(handle, "IsProfilerAttached", out var export))
            {
                // Loading an unattached copy succeeds too; only the native instance can confirm attachment.
                return Marshal.GetDelegateForFunctionPointer<IsProfilerAttachedDelegate>(export)();
            }
        }
        catch (Exception)
        {
            // Missing or incompatible native assets must not prevent the application from starting.
        }
        finally
        {
            if (handle != 0)
            {
                // Balance only our load reference. The CLR or proxy retains the attached profiler's reference.
                NativeLibrary.Free(handle);
            }
        }

        return false;
    }
}
#endif
