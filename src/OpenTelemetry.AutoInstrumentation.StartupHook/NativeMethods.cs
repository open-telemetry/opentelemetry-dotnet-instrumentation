// <copyright file="NativeMethods.cs" company="OpenTelemetry Authors">
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

using System.Reflection;
using System.Runtime.InteropServices;
using OpenTelemetry.AutoInstrumentation.Helpers;

namespace OpenTelemetry.AutoInstrumentation;

internal static class NativeMethods
{
    static NativeMethods()
    {
        NativeLibrary.SetDllImportResolver(typeof(NativeMethods).Assembly, ImportResolver);
    }

    [DllImport("OpenTelemetry.AutoInstrumentation.Native", CallingConvention = CallingConvention.Cdecl)]
    public static extern bool IsProfilerAttached();

    private static IntPtr ImportResolver(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
    {
        IntPtr libHandle = IntPtr.Zero;

        if (libraryName == "OpenTelemetry.AutoInstrumentation.Native")
        {
            var homePath = EnvironmentHelper.GetEnvironmentVariable("OTEL_DOTNET_AUTO_HOME")!;
            string archDir;
            string extension;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                archDir = Environment.Is64BitProcess ? "win-x64" : "win-x86";
                extension = "dll";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                archDir = Environment.Is64BitProcess ? GetLinuxDir(homePath) : throw new PlatformNotSupportedException("32bit Linux is not supported.");
                extension = "so";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                archDir = Environment.Is64BitProcess ? "osx-x64" : throw new PlatformNotSupportedException("32bit MacOS is not supported.");
                extension = "dylib";
            }
            else
            {
                throw new NotSupportedException("Platfrom is not supported");
            }

            libHandle = NativeLibrary.Load(Path.Combine(homePath, archDir, $"OpenTelemetry.AutoInstrumentation.Native.{extension}"));
        }

        return libHandle;
    }

    private static string GetLinuxDir(string homePath)
    {
        if (Directory.Exists(Path.Combine(homePath, "linux-x64")))
        {
            return "linux-x64";
        }

        if (Directory.Exists(Path.Combine(homePath, "linux-musl-x64")))
        {
            return "linux-musl-x64";
        }

        throw new PlatformNotSupportedException("Could not determine Linux platform.");
    }
}
