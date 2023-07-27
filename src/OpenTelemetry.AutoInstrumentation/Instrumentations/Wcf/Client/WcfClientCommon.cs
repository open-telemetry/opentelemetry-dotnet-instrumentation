// <copyright file="WcfClientCommon.cs" company="OpenTelemetry Authors">
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

#if NETFRAMEWORK
using System.Diagnostics;

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.Wcf.Client;

internal static class WcfClientCommon
{
    private static readonly ActivitySource Source = new("OpenTelemetry.AutoInstrumentation.Wcf", AutoInstrumentationVersion.Version);

    private static readonly string OutgoingActivityName = $"{Source.Name}.OutgoingActivity";

    internal static Activity? StartActivity()
    {
        return Source.StartActivity(OutgoingActivityName, ActivityKind.Client);
    }
}
#endif
