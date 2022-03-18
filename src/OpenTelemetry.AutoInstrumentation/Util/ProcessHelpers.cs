// <copyright file="ProcessHelpers.cs" company="OpenTelemetry Authors">
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
using System.Runtime.CompilerServices;

namespace OpenTelemetry.AutoInstrumentation.Util;

internal static class ProcessHelpers
{
    /// <summary>
    /// Wrapper around <see cref="Process.GetCurrentProcess"/> and its property accesses
    ///
    /// On .NET Framework the <see cref="Process"/> class is guarded by a
    /// LinkDemand for FullTrust, so partial trust callers will throw an exception.
    /// This exception is thrown when the caller method is being JIT compiled, NOT
    /// when Process.GetCurrentProcess is called, so this wrapper method allows
    /// us to catch the exception.
    /// </summary>
    /// <param name="processName">The name of the current process</param>
    /// <param name="machineName">The machine name of the current process</param>
    /// <param name="processId">The ID of the current process</param>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void GetCurrentProcessInformation(out string processName, out string machineName, out int processId)
    {
        using (var currentProcess = Process.GetCurrentProcess())
        {
            processName = currentProcess.ProcessName;
            machineName = currentProcess.MachineName;
            processId = currentProcess.Id;
        }
    }
}
