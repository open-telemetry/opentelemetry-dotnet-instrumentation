using System;
using System.Collections.Generic;
using System.Linq;
using Datadog.Trace.Headers;
using Datadog.Trace.Propagation;
using Datadog.Trace.TestHelpers;
using Xunit;

namespace Datadog.Trace.Tests.Propagators
{
    public class MultiplexSpanContextPropagatorTests : HeadersCollectionTestBase
    {
        private readonly IPropagator _multiplexer;

        public MultiplexSpanContextPropagatorTests()
        {
            _multiplexer = new MultiplexSpanContextPropagator(
                new[]
                {
                    new ExamplePropagatorStub("propagator-1", "propagation-1", canExtract: false),
                    new ExamplePropagatorStub("propagator-2", "propagation-2", canExtract: true),
                    new ExamplePropagatorStub("propagator-3", "propagation-3", canExtract: true)
                });
        }

        [Theory]
        [MemberData(nameof(GetHeaderCollectionImplementations))]
        internal void InjectExtract(IHeadersCollection headers)
        {
            _multiplexer.Inject(null, headers);

            AssertExpected(headers, "propagator-1", "propagation-1");
            AssertExpected(headers, "propagator-2", "propagation-2");
            AssertExpected(headers, "propagator-3", "propagation-3");

            SpanContext context = _multiplexer.Extract(headers);

            // First propagator that meets the requirements extracts the context
            Assert.Equal("propagation-2", context.ServiceName);
        }

        private class ExamplePropagatorStub : IPropagator
        {
            private readonly string _header;
            private readonly string _value;
            private readonly bool _canExtract;

            public ExamplePropagatorStub(string header, string value, bool canExtract)
            {
                _header = header;
                _value = value;
                _canExtract = canExtract;
            }

            public SpanContext Extract<T>(T carrier, Func<T, string, IEnumerable<string>> getter)
            {
                if (!_canExtract)
                {
                    return null;
                }

                string value = getter(carrier, _header).First();

                return new SpanContext(null, null, value);
            }

            public void Inject<T>(SpanContext context, T carrier, Action<T, string, string> setter)
            {
                setter(carrier, _header, _value);
            }
        }
    }
}
