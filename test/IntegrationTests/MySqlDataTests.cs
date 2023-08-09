// <copyright file="MySqlDataTests.cs" company="OpenTelemetry Authors">
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

#if NET6_0_OR_GREATER
using Google.Protobuf;
using IntegrationTests.Helpers;
using OpenTelemetry.Proto.Trace.V1;
using Xunit.Abstractions;

namespace IntegrationTests;

[Collection(MySqlCollection.Name)]
[UsesVerify]
public class MySqlDataTests : TestHelper
{
    private readonly MySqlFixture _mySql;

    public MySqlDataTests(ITestOutputHelper output, MySqlFixture mySql)
        : base("MySqlData", output)
    {
        _mySql = mySql;
    }

    [Theory]
    [Trait("Category", "EndToEnd")]
    [Trait("Containers", "Linux")]
    [MemberData(nameof(LibraryVersion.MySqlData), MemberType = typeof(LibraryVersion))]
    public async Task SubmitsTraces(string packageVersion)
    {
        using var collector = new MockSpansCollector(Output);
        SetExporter(collector);
        collector.Expect("OpenTelemetry.Instrumentation.MySqlData");

        EnableBytecodeInstrumentation();
        RunTestApplication(new()
        {
            Arguments = $"--mysql {_mySql.Port}",
            PackageVersion = packageVersion
        });

        var spans = collector.GetSpans(timeout: TimeSpan.FromSeconds(5));

        var settings = new VerifySettings();
        settings.IgnoreMember<Span>(x => x.StartTimeUnixNano);
        settings.IgnoreMember<Span>(x => x.EndTimeUnixNano);
        settings.AddExtraSettings(_ => _.Converters.Add(new SpanConverter()));
        settings.AddExtraSettings(_ => _.Converters.Add(new KeyValueConverter(x => x.Replace(_mySql.Port.ToString(), "3306"))));

        await Verifier.Verify(spans, settings)
                      .UseFileName(nameof(MySqlDataTests));
    }

    private class SpanConverter : WriteOnlyJsonConverter<Span>
    {
        private readonly Dictionary<ByteString, string> _cache = new();
        private int _counter = 1;

        public override void Write(VerifyJsonWriter writer, Span value)
        {
            writer.WriteStartObject();
            writer.WriteMember(value, NormalizeByteString(value.TraceId), "TraceId");
            writer.WriteMember(value, NormalizeByteString(value.SpanId), "SpanId");
            writer.WriteMember(value, value.TraceState, "TraceState");
            writer.WriteMember(value, NormalizeByteString(value.ParentSpanId), "ParentSpanId");
            writer.WriteMember(value, value.Name, "Name");
            writer.WriteMember(value, value.Kind, "Kind");
            writer.WriteMember(value, value.Attributes, "Attributes");
            writer.WriteEndObject();
        }

        public string NormalizeByteString(ByteString byteString)
        {
            if (byteString.Length == 0)
            {
                return string.Empty;
            }

            if (!_cache.TryGetValue(byteString, out var cachedString))
            {
                cachedString = $"Id_{_counter}";
                _cache.Add(byteString, cachedString);
                _counter++;
            }

            return cachedString;
        }
    }

    private class KeyValueConverter : WriteOnlyJsonConverter<OpenTelemetry.Proto.Common.V1.KeyValue>
    {
        private Func<string, string> _valueStringReplace;

        public KeyValueConverter(Func<string, string> valueStringReplace)
        {
            _valueStringReplace = valueStringReplace;
        }

        public override void Write(VerifyJsonWriter writer, OpenTelemetry.Proto.Common.V1.KeyValue value)
        {
            writer.WriteStartObject();
            writer.WriteMember(value, value.Key, "Key");
            writer.WriteMember(value, _valueStringReplace(value.Value.ToString()), "Value");
            writer.WriteEndObject();
        }
    }
}
#endif
