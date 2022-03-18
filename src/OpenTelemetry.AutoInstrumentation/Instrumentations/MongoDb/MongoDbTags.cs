// <copyright file="MongoDbTags.cs" company="OpenTelemetry Authors">
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

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.MongoDb;

internal class MongoDbTags : InstrumentationTags
{
    protected static readonly IProperty<string>[] MongoDbTagsProperties =
        InstrumentationTagsProperties.Concat(
            new ReadOnlyProperty<MongoDbTags, string>(Tags.InstrumentationName, t => t.InstrumentationName),
            new Property<MongoDbTags, string>(Tags.MongoDb.Name, t => t.DbName, (t, v) => t.DbName = v),
            new Property<MongoDbTags, string>(Tags.MongoDb.Query, t => t.Query, (t, v) => t.Query = v),
            new Property<MongoDbTags, string>(Tags.MongoDb.Collection, t => t.Collection, (t, v) => t.Collection = v),
            new Property<MongoDbTags, string>(Tags.OutHost, t => t.Host, (t, v) => t.Host = v),
            new Property<MongoDbTags, string>(Tags.OutPort, t => t.Port, (t, v) => t.Port = v));

    public override ActivityKind Kind => ActivityKind.Client;

    public string InstrumentationName => MongoDbIntegration.IntegrationName;

    public string DbName { get; set; }

    public string Query { get; set; }

    public string Collection { get; set; }

    public string Host { get; set; }

    public string Port { get; set; }

    protected override IProperty<string>[] GetAdditionalTags() => MongoDbTagsProperties;
}
