// <copyright file="CommonTags.cs" company="OpenTelemetry Authors">
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

namespace OpenTelemetry.AutoInstrumentation.Tagging;

internal class CommonTags : TagsList
{
    protected static readonly IProperty<string>[] CommonTagsProperties =
    {
        new Property<CommonTags, string>(Tags.Env, t => t.Environment, (t, v) => t.Environment = v),
        new Property<CommonTags, string>(Tags.Version, t => t.Version, (t, v) => t.Version = v)
    };

    public string Environment { get; set; }

    public string Version { get; set; }

    protected override IProperty<string>[] GetAdditionalTags() => CommonTagsProperties;
}
