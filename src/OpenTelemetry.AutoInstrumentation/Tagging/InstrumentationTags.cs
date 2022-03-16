// <copyright file="InstrumentationTags.cs" company="OpenTelemetry Authors">
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
using OpenTelemetry.AutoInstrumentation.Util;

namespace OpenTelemetry.AutoInstrumentation.Tagging;

internal abstract class InstrumentationTags : CommonTags
{
    protected static readonly IProperty<string>[] InstrumentationTagsProperties =
        CommonTagsProperties.Concat(
            new ReadOnlyProperty<InstrumentationTags, string>(Tags.SpanKind, t => t.Kind.ToString()));

    public abstract ActivityKind Kind { get; }

    protected override IProperty<string>[] GetAdditionalTags() => InstrumentationTagsProperties;
}
