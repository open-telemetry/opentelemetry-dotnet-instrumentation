// <copyright file="ServiceNameConfigurator.cs" company="OpenTelemetry Authors">
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

using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace OpenTelemetry.AutoInstrumentation.Configurations;

internal static class ServiceNameConfigurator
{
    internal static string GetFallbackServiceName()
    {
#if NETFRAMEWORK
        // System.Web.dll is only available on .NET Framework
        if (System.Web.Hosting.HostingEnvironment.IsHosted)
        {
            // if this app is an ASP.NET application, return "SiteName/ApplicationVirtualPath".
            // note that ApplicationVirtualPath includes a leading slash.
            return (System.Web.Hosting.HostingEnvironment.SiteName + System.Web.Hosting.HostingEnvironment.ApplicationVirtualPath).TrimEnd('/');
        }
#endif
        return Assembly.GetEntryAssembly()?.GetName().Name ?? GetCurrentProcessName();
    }

    /// <summary>
    /// <para>Wrapper around <see cref="Process.GetCurrentProcess"/> and <see cref="Process.ProcessName"/></para>
    /// <para>
    /// On .NET Framework the <see cref="Process"/> class is guarded by a
    /// LinkDemand for FullTrust, so partial trust callers will throw an exception.
    /// This exception is thrown when the caller method is being JIT compiled, NOT
    /// when Process.GetCurrentProcess is called, so this wrapper method allows
    /// us to catch the exception.
    /// </para>
    /// </summary>
    /// <returns>Returns the name of the current process.</returns>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static string GetCurrentProcessName()
    {
        using var currentProcess = Process.GetCurrentProcess();
        return currentProcess.ProcessName;
    }
}
