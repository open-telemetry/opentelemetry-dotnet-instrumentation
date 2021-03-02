using System;
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
        [InlineData("https://username:password@example.com/path/to/file.aspx?query=1#fragment", "https://example.com/path/to/file.aspx?query=1#fragment")]
        [InlineData("https://username@example.com/path/to/file.aspx", "https://example.com/path/to/file.aspx")]
        [InlineData("https://example.com/path/to/file.aspx?query=1", "https://example.com/path/to/file.aspx?query=1")]
        [InlineData("https://example.com/path/to/file.aspx#fragment", "https://example.com/path/to/file.aspx#fragment")]
        [InlineData("http://example.com/path/to/file.aspx", "http://example.com/path/to/file.aspx")]
        [InlineData("https://example.com/path/123/file.aspx", "https://example.com/path/123/file.aspx")]
        [InlineData("https://example.com/path/123/", "https://example.com/path/123/")]
        [InlineData("https://example.com/path/123", "https://example.com/path/123")]
        [InlineData("https://example.com/path/E653C852-227B-4F0C-9E48-D30D83C68BF3", "https://example.com/path/E653C852-227B-4F0C-9E48-D30D83C68BF3")]
        [InlineData("https://example.com/path/E653C852227B4F0C9E48D30D83C68BF3", "https://example.com/path/E653C852227B4F0C9E48D30D83C68BF3")]
        public void OutboundHttp_HttpUrlTag(string uri, string expected)
        {
            var settings = new TracerSettings();
            settings.Convention = ConventionType.OpenTelemetry;
            var tracer = new Tracer(settings, Mock.Of<IAgentWriter>(), Mock.Of<ISampler>(), scopeManager: null, statsd: null);

            const string method = "GET";
            using (var automaticScope = ScopeFactory.CreateOutboundHttpScope(tracer, method, new Uri(uri), new IntegrationInfo((int)IntegrationIds.HttpMessageHandler), out var tags))
            {
                Assert.Equal(expected, automaticScope.Span.GetTag(Tags.HttpUrl));
                Assert.Equal(expected, tags.HttpUrl);
            }
        }
    }
}
