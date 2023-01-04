// <copyright file="Target.cs" company="OpenTelemetry Authors">
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

using System.Text.Json.Serialization;

namespace IntegrationsJsonGenerator;

internal class Target
{
    [JsonPropertyName("assembly")]
    public string? Assembly { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("method")]
    public string? Method { get; set; }

    [JsonPropertyName("signature_types")]
    public string[]? SignatureTypes { get; set; }

    [JsonPropertyName("minimum_major")]
    public int MinimumMajor { get; set; }

    [JsonPropertyName("minimum_minor")]
    public int MinimumMinor { get; set; }

    [JsonPropertyName("minimum_patch")]
    public int MinimumPath { get; set; }

    [JsonPropertyName("maximum_major")]
    public int MaximumMajor { get; set; }

    [JsonPropertyName("maximum_minor")]
    public int MaximumMinor { get; set; } = 65535;

    [JsonPropertyName("maximum_patch")]
    public int MaximumPath { get; set; } = 65535;
}
