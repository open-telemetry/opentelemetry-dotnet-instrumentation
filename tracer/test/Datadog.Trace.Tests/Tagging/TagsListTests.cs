using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Datadog.Trace.Agent.MessagePack;
using Datadog.Trace.ClrProfiler.Integrations.AdoNet;
using Datadog.Trace.Tagging;
using Datadog.Trace.Util;
using Datadog.Trace.Vendors.MessagePack;
using Moq;
using Xunit;

namespace Datadog.Trace.Tests.Tagging
{
    public class TagsListTests
    {
        [Fact]
        public void SetTag_WillNotCauseDuplicates()
        {
            // Initialize common tags
            var tags = new CommonTags()
            {
                Version = "v1.0",
                Environment = "Test"
            };

            // Initialize custom tags
            tags.SetTag("sample.1", "Temp 1");
            tags.SetTag("sample.2", "Temp 2");

            // Try set existing tag
            tags.SetTag(Tags.Version, "v2.0");
            tags.SetTag("sample.2", "Temp 3");

            var all = tags.GetAllTags();
            var distinctKeys = all.Select(x => x.Key).Distinct().Count();

            Assert.Equal(all.Count, distinctKeys);
            Assert.Single(all, x => x.Key == Tags.Version && x.Value == "v2.0");
            Assert.Single(all, x => x.Key == "sample.2" && x.Value == "Temp 3");
        }

        [Fact]
        public void GetAll()
        {
            // Should be any actual implementation
            var tags = new CommonTags();
            var values = new[]
            {
                "v1.0", "Test", "value 1", "value 2"
            };

            tags.Version = values[0];
            tags.Environment = values[1];

            tags.SetTag("sample.1", values[2]);
            tags.SetTag("sample.2", values[3]);

            ValidateTags(tags.GetAllTags(), values);
        }

        [Fact]
        public void GetAll_When_MissingTags()
        {
            var tags = new EmptyTags();
            var values = ArrayHelper.Empty<string>();

            ValidateTags(tags.GetAllTags(), values);
        }

        [Fact]
        public void CheckProperties()
        {
            var assemblies = new[] { typeof(TagsList).Assembly, typeof(SqlTags).Assembly };

            foreach (var type in assemblies.SelectMany(a => a.GetTypes()))
            {
                if (!typeof(TagsList).IsAssignableFrom(type))
                {
                    continue;
                }

                if (type.IsInterface || type.IsAbstract)
                {
                    continue;
                }

                var random = new Random();

                ValidateProperties<string>(type, "GetAdditionalTags", () => Guid.NewGuid().ToString());
                ValidateProperties<double?>(type, "GetAdditionalMetrics", () => random.NextDouble());
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Serialization(bool topLevelSpan)
        {
            var tags = new CommonTags();

            Span span;

            if (topLevelSpan)
            {
                span = new Span(new SpanContext(TraceId.CreateFromInt(42), 41), DateTimeOffset.UtcNow, tags);
            }
            else
            {
                // Assign a parent to prevent the span from being considered as top-level
                var traceContext = new TraceContext(Mock.Of<IDatadogTracer>());
                var parent = new SpanContext(TraceId.CreateFromInt(42), 41);
                span = new Span(new SpanContext(parent, traceContext, null), DateTimeOffset.UtcNow, tags);
            }

            // The span has 1 "common" tag and 15 additional tags (and same number of metrics)
            // Those numbers are picked to test the variable-size header of MessagePack
            // The header is resized when there are 16 or more elements in the collection
            // Neither common or additional tags have enough elements, but put together they will cause to use a bigger header
            tags.Environment = "Test";
            tags.SamplingLimitDecision = 0.5;

            for (int i = 0; i < 15; i++)
            {
                span.SetTag(i.ToString(), i.ToString());
            }

            for (int i = 0; i < 15; i++)
            {
                span.SetMetric(i.ToString(), i);
            }

            var buffer = new byte[0];

            var resolver = new FormatterResolverWrapper(SpanFormatterResolver.Instance);
            MessagePackSerializer.Serialize(ref buffer, 0, span, resolver);

            var deserializedSpan = MessagePack.MessagePackSerializer.Deserialize<FakeSpan>(buffer);

            // For top-level spans, there is one tag added during serialization
            Assert.Equal(topLevelSpan ? 17 : 16, deserializedSpan.Tags.Count);

            // For top-level spans, there is one metric added during serialization
            Assert.Equal(topLevelSpan ? 17 : 16, deserializedSpan.Metrics.Count);

            Assert.Equal("Test", deserializedSpan.Tags[Tags.Env]);
            Assert.Equal(0.5, deserializedSpan.Metrics[Metrics.SamplingLimitDecision]);

            for (int i = 0; i < 15; i++)
            {
                Assert.Equal(i.ToString(), deserializedSpan.Tags[i.ToString()]);
                Assert.Equal((double)i, deserializedSpan.Metrics[i.ToString()]);
            }

            if (topLevelSpan)
            {
                Assert.Equal(Tracer.RuntimeId, deserializedSpan.Tags[Tags.RuntimeId]);
                Assert.Equal(1.0, deserializedSpan.Metrics[Metrics.TopLevelSpan]);
            }
        }

        private void ValidateProperties<T>(Type type, string methodName, Func<T> valueGenerator)
        {
            var instance = (ITags)Activator.CreateInstance(type, nonPublic: true);

            var allTags = (IProperty<T>[])type.GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(instance, null);

            var tags = allTags.Where(t => !t.IsReadOnly).ToArray();
            var readonlyTags = allTags.Where(t => t.IsReadOnly).ToArray();

            var allProperties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(p => p.PropertyType == typeof(T))
                .ToArray();

            var properties = allProperties.Where(p => p.CanWrite).ToArray();
            var readonlyProperties = allProperties.Where(p => !p.CanWrite).ToArray();

            Assert.True(properties.Length == tags.Length, $"Mismatch between readonly properties and tags count for type {type}");
            Assert.True(readonlyProperties.Length == readonlyTags.Length, $"Mismatch between readonly properties and tags count for type {type}");

            // ---------- Test read-write properties
            var testValues = Enumerable.Range(0, tags.Length).Select(_ => valueGenerator()).ToArray();

            // Check for each tag that the getter and the setter are mapped on the same property
            for (int i = 0; i < tags.Length; i++)
            {
                var tag = tags[i];

                tag.Setter(instance, testValues[i]);

                Assert.True(testValues[i].Equals(tag.Getter(instance)), $"Getter and setter mismatch for tag {tag.Key} of type {type.Name}");
            }

            // Check that all read/write properties were mapped
            var remainingValues = new HashSet<T>(testValues);

            foreach (var property in properties)
            {
                Assert.True(remainingValues.Remove((T)property.GetValue(instance)), $"Property {property.Name} of type {type.Name} is not mapped");
            }

            // ---------- Test readonly properties
            remainingValues = new HashSet<T>(readonlyProperties.Select(p => (T)p.GetValue(instance)));

            foreach (var tag in readonlyTags)
            {
                Assert.True(remainingValues.Remove(tag.Getter(instance)), $"Tag {tag.Key} of type {type.Name} is not mapped");
            }
        }

        private void ValidateTags(List<KeyValuePair<string, string>> tags, string[] values)
        {
            Assert.True(tags.Count >= values.Length); // At least specified values

            if (values.Length > 0)
            {
                Assert.Contains(values, v => values.Contains(v));
            }
        }

        [MessagePack.MessagePackObject]
        public struct FakeSpan
        {
            [MessagePack.Key("trace_id")]
            public ulong TraceId { get; set; }

            [MessagePack.Key("span_id")]
            public ulong SpanId { get; set; }

            [MessagePack.Key("name")]
            public string Name { get; set; }

            [MessagePack.Key("resource")]
            public string Resource { get; set; }

            [MessagePack.Key("service")]
            public string Service { get; set; }

            [MessagePack.Key("type")]
            public string Type { get; set; }

            [MessagePack.Key("start")]
            public long Start { get; set; }

            [MessagePack.Key("duration")]
            public long Duration { get; set; }

            [MessagePack.Key("parent_id")]
            public ulong? ParentId { get; set; }

            [MessagePack.Key("error")]
            public byte Error { get; set; }

            [MessagePack.Key("meta")]
            public Dictionary<string, string> Tags { get; set; }

            [MessagePack.Key("metrics")]
            public Dictionary<string, double> Metrics { get; set; }
        }

        internal class EmptyTags : TagsList
        {
            protected override IProperty<string>[] GetAdditionalTags()
            {
                // custom logic with possibility of null return
                return null;
            }
        }
    }
}
