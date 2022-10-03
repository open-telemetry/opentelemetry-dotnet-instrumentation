// <copyright file="IMockSpan.cs" company="OpenTelemetry Authors">
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

using System.Collections.Generic;
using System.Diagnostics;

namespace IntegrationTests.Helpers.Mocks;

public interface IMockSpan
{
    public string TraceId { get; }

    public ulong SpanId { get; }

    public string Name { get; }

    public string Service { get; }

    public string Library { get; }

    public ActivityKind Kind { get; }

    public long Start { get; }

    public ulong? ParentId { get; }

    public byte Error { get; }

    public Dictionary<string, string> Tags { get; }
}
