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

namespace IntegrationTests.Helpers.Mocks;

public interface IMockSpan
{
    public string TraceId { get; }

    public ulong SpanId { get; }

    public string Name { get; set; }

    public string Resource { get; set; }

    public string Service { get; }

    public string Type { get; set; }

    public long Start { get; }

    public long Duration { get; set; }

    public ulong? ParentId { get; }

    public byte Error { get; set; }

    public Dictionary<string, string> Tags { get; set; }
}
