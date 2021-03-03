using System;
using System.Text;
using Datadog.Trace.Agent;
using Datadog.Trace.Configuration;
using Datadog.Trace.Sampling;
using Moq;
using Xunit;

namespace Datadog.Trace.ClrProfiler.Managed.Tests
{
    public class OtelScopeFactoryTests
    {
        [Theory]
        [ClassData(typeof(TestData))]
        public void OutboundHttp(Input input, Result expected)
        {
            var settings = new TracerSettings();
            settings.Convention = ConventionType.OpenTelemetry;
            var tracer = new Tracer(settings, Mock.Of<IAgentWriter>(), Mock.Of<ISampler>(), scopeManager: null, statsd: null);

            using (var scope = ScopeFactory.CreateOutboundHttpScope(tracer, input.Method, new Uri(input.Uri), new IntegrationInfo((int)IntegrationIds.HttpMessageHandler), out var tags))
            {
                var result = new Result
                {
                    HttpMethodTag = scope.Span.GetTag("http.method"),
                    HttpMethodProperty = tags.HttpMethod,

                    HttpUrlTag = scope.Span.GetTag("http.url"),
                    HttpUrlProperty = tags.HttpUrl,

                    HttpTargetTag = scope.Span.GetTag("http.target")
                };

                Assert.Equal(expected, result);
            }
        }

        public struct Input
        {
            public string Method;
            public string Uri;
        }

        public struct Result
        {
            public string HttpMethodProperty;
            public string HttpMethodTag;
            public string HttpUrlProperty;
            public string HttpUrlTag;
            public string HttpTargetTag;

            public override string ToString()
            {
                var sb = new StringBuilder();
                sb.Append(Environment.NewLine);
                foreach (var field in GetType().GetFields())
                {
                    sb.Append($"{field.Name}: {field.GetValue(this)}{Environment.NewLine}");
                }

                return sb.ToString();
            }
        }

        public class TestData : TheoryData<Input, Result>
        {
#pragma warning disable SA1118 // The parameter spans multiple lines
            public TestData()
            {
                Add(
                    new Input
                    {
                        Method = "GET",
                        Uri = "https://username:password@example.com/path/to/file.aspx?query=1#fragment",
                    },
                    new Result
                    {
                        HttpMethodTag = "GET",
                        HttpMethodProperty = "GET",
                        HttpUrlProperty = "https://example.com/path/to/file.aspx?query=1#fragment",
                        HttpUrlTag = "https://example.com/path/to/file.aspx?query=1#fragment",
                        HttpTargetTag = "/path/to/file.aspx?query=1#fragment",
                    });
                Add(
                    new Input
                    {
                        Method = "GET",
                        Uri = "https://username@example.com/path/to/file.aspx",
                    },
                    new Result
                    {
                        HttpMethodTag = "GET",
                        HttpMethodProperty = "GET",
                        HttpUrlProperty = "https://example.com/path/to/file.aspx",
                        HttpUrlTag = "https://example.com/path/to/file.aspx",
                        HttpTargetTag = "/path/to/file.aspx",
                    });
                Add(
                    new Input
                    {
                        Method = "GET",
                        Uri = "https://example.com/path/to/file.aspx?query=1",
                    },
                    new Result
                    {
                        HttpMethodTag = "GET",
                        HttpMethodProperty = "GET",
                        HttpUrlProperty = "https://example.com/path/to/file.aspx?query=1",
                        HttpUrlTag = "https://example.com/path/to/file.aspx?query=1",
                        HttpTargetTag = "/path/to/file.aspx?query=1",
                    });
                Add(
                    new Input
                    {
                        Method = "GET",
                        Uri = "https://example.com/path/to/file.aspx#fragment",
                    },
                    new Result
                    {
                        HttpMethodTag = "GET",
                        HttpMethodProperty = "GET",
                        HttpUrlProperty = "https://example.com/path/to/file.aspx#fragment",
                        HttpUrlTag = "https://example.com/path/to/file.aspx#fragment",
                        HttpTargetTag = "/path/to/file.aspx#fragment",
                    });
                Add(
                    new Input
                    {
                        Method = "GET",
                        Uri = "https://example.com/path/to/file.aspx",
                    },
                    new Result
                    {
                        HttpMethodTag = "GET",
                        HttpMethodProperty = "GET",
                        HttpUrlProperty = "https://example.com/path/to/file.aspx",
                        HttpUrlTag = "https://example.com/path/to/file.aspx",
                        HttpTargetTag = "/path/to/file.aspx",
                    });
                Add(
                    new Input
                    {
                        Method = "GET",
                        Uri = "https://example.com/path/123/file.aspx",
                    },
                    new Result
                    {
                        HttpMethodTag = "GET",
                        HttpMethodProperty = "GET",
                        HttpUrlProperty = "https://example.com/path/123/file.aspx",
                        HttpUrlTag = "https://example.com/path/123/file.aspx",
                        HttpTargetTag = "/path/123/file.aspx",
                    });
                Add(
                    new Input
                    {
                        Method = "GET",
                        Uri = "https://example.com/path/123/",
                    },
                    new Result
                    {
                        HttpMethodTag = "GET",
                        HttpMethodProperty = "GET",
                        HttpUrlProperty = "https://example.com/path/123/",
                        HttpUrlTag = "https://example.com/path/123/",
                        HttpTargetTag = "/path/123/",
                    });
                Add(
                    new Input
                    {
                        Method = "GET",
                        Uri = "https://example.com/path/123",
                    },
                    new Result
                    {
                        HttpMethodTag = "GET",
                        HttpMethodProperty = "GET",
                        HttpUrlProperty = "https://example.com/path/123",
                        HttpUrlTag = "https://example.com/path/123",
                        HttpTargetTag = "/path/123",
                    });
                Add(
                    new Input
                    {
                        Method = "GET",
                        Uri = "https://example.com/path/E653C852-227B-4F0C-9E48-D30D83C68BF3",
                    },
                    new Result
                    {
                        HttpMethodTag = "GET",
                        HttpMethodProperty = "GET",
                        HttpUrlProperty = "https://example.com/path/E653C852-227B-4F0C-9E48-D30D83C68BF3",
                        HttpUrlTag = "https://example.com/path/E653C852-227B-4F0C-9E48-D30D83C68BF3",
                        HttpTargetTag = "/path/E653C852-227B-4F0C-9E48-D30D83C68BF3",
                    });
                Add(
                    new Input
                    {
                        Method = "GET",
                        Uri = "https://example.com/path/E653C852227B4F0C9E48D30D83C68BF3",
                    },
                    new Result
                    {
                        HttpMethodTag = "GET",
                        HttpMethodProperty = "GET",
                        HttpUrlProperty = "https://example.com/path/E653C852227B4F0C9E48D30D83C68BF3",
                        HttpUrlTag = "https://example.com/path/E653C852227B4F0C9E48D30D83C68BF3",
                        HttpTargetTag = "/path/E653C852227B4F0C9E48D30D83C68BF3",
                    });
            }
#pragma warning restore SA1118
        }
    }
}
