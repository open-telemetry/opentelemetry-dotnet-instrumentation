// <copyright file="GraphQLTags.cs" company="OpenTelemetry Authors">
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
using OpenTelemetry.AutoInstrumentation.Tagging;
using OpenTelemetry.AutoInstrumentation.Util;

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.GraphQL;

internal class GraphQLTags : InstrumentationTags
{
    protected static readonly IProperty<string>[] GraphQLTagsProperties =
        InstrumentationTagsProperties.Concat(
            new ReadOnlyProperty<GraphQLTags, string>(Tags.InstrumentationName, t => t.InstrumentationName),
            new Property<GraphQLTags, string>(Tags.GraphQL.Source, t => t.Source, (t, v) => t.Source = v),
            new Property<GraphQLTags, string>(Tags.GraphQL.OperationName, t => t.OperationName, (t, v) => t.OperationName = v),
            new Property<GraphQLTags, string>(Tags.GraphQL.OperationType, t => t.OperationType, (t, v) => t.OperationType = v),
            new ReadOnlyProperty<GraphQLTags, string>(Tags.Language, t => t.Language));

    public override ActivityKind Kind => ActivityKind.Server;

    public string InstrumentationName => GraphQLCommon.IntegrationName;

    public string Language => Constants.Tracer.Language;

    public string Source { get; set; }

    public string OperationName { get; set; }

    public string OperationType { get; set; }

    protected override IProperty<string>[] GetAdditionalTags() => GraphQLTagsProperties;
}
