// <copyright file="AutoInstrumentationEventSource.cs" company="OpenTelemetry Authors">
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

using System.Diagnostics.Tracing;

namespace OpenTelemetry.AutoInstrumentation;

/// <summary>
/// EventSource implementation for OpenTelemetry AutoInstrumentation implementation.
/// </summary>
[EventSource(Name = "OpenTelemetry-AutoInstrumentation")]
internal class AutoInstrumentationEventSource : EventSource
{
#pragma warning disable SA1401 // Fields should be private
    public static readonly AutoInstrumentationEventSource Log = new AutoInstrumentationEventSource();
#pragma warning restore SA1401 // Fields should be private

    /// <summary>Logs as Trace level message.</summary>
    /// <param name="message">Message to log.</param>
    [Event(1, Message = "{0}", Level = EventLevel.Informational)]
    public void Trace(string message)
    {
        this.WriteEvent(1, message);
    }

    /// <summary>Logs as Error level message.</summary>
    /// <param name="message">Error to log.</param>
    [Event(2, Message = "{0}", Level = EventLevel.Error)]
    public void Error(string message)
    {
        this.WriteEvent(2, message);
    }
}
