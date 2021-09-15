using System;
using FluentAssertions;
using FluentAssertions.Execution;
using Xunit;

namespace Datadog.Trace.Tests
{
    public class TraceIdTests
    {
        [Fact]
        public void CreateRandom_CreatesValid128BitId()
        {
            var traceId = TraceId.CreateRandom();

            using (new AssertionScope())
            {
                traceId.ToString().Should().HaveLength(32);
                FluentActions.Invoking(() => Convert.ToUInt64(traceId.ToString().Substring(startIndex: 0, length: 16), fromBase: 16)).Should().NotThrow();
                FluentActions.Invoking(() => Convert.ToUInt64(traceId.ToString().Substring(startIndex: 16, length: 16), fromBase: 16)).Should().NotThrow();
            }
        }

        [Fact]
        public void CreateFromString_CreatesIdCorrectly()
        {
            var traceId = TraceId.CreateRandom();
            var recreatedId = TraceId.CreateFromString(traceId.ToString());

            recreatedId.Should().Be(traceId);
        }

        [Fact]
        public void CreateFromInt_CreatesIdCorrectly()
        {
            var traceId = TraceId.CreateFromInt(123);

            traceId.ToString().Should().Be("123");
        }

        [Fact]
        public void CreateFromUlong_CreatesIdCorrectly()
        {
            var traceId = TraceId.CreateFromUlong(3212132132132132121);

            traceId.ToString().Should().Be("3212132132132132121");
        }

        [Fact]
        public void CreateRandom64Bit_CreatesValid64BitId()
        {
            var traceId = TraceId.CreateRandomDataDogCompatible();

            using (new AssertionScope())
            {
                FluentActions.Invoking(() => Convert.ToUInt64(traceId.ToString(), fromBase: 10)).Should().NotThrow();
            }
        }

        [Fact]
        public void Lower_Returns64LowerBitsOfId()
        {
            var traceId = TraceId.CreateRandom();

            traceId.Lower.Should().Be(Convert.ToInt64(traceId.ToString().Substring(startIndex: 16, length: 16), fromBase: 16));
        }

        [Fact]
        public void Equals_WorksCorrectlyFor128BitId()
        {
            var traceId1 = TraceId.CreateRandom();
            var traceId2 = TraceId.CreateRandom();

            using (new AssertionScope())
            {
                traceId1.Should().Be(TraceId.CreateFromString(traceId1.ToString()));
                traceId2.Should().Be(TraceId.CreateFromString(traceId2.ToString()));
                traceId1.Should().NotBe(TraceId.CreateFromString(traceId2.ToString()));
                traceId2.Should().NotBe(TraceId.CreateFromString(traceId1.ToString()));
            }
        }

        [Fact]
        public void Equals_WorksCorrectlyFor64BitId()
        {
            var traceId1 = TraceId.CreateRandomDataDogCompatible();
            var traceId2 = TraceId.CreateRandomDataDogCompatible();

            using (new AssertionScope())
            {
                traceId1.Should().Be(TraceId.CreateDataDogCompatibleFromDecimalString(traceId1.ToString()));
                traceId1.GetHashCode().Should().Be(TraceId.CreateDataDogCompatibleFromDecimalString(traceId1.ToString()).GetHashCode());
                traceId2.Should().Be(TraceId.CreateDataDogCompatibleFromDecimalString(traceId2.ToString()));
                traceId2.GetHashCode().Should().Be(TraceId.CreateDataDogCompatibleFromDecimalString(traceId2.ToString()).GetHashCode());
                traceId1.Should().NotBe(TraceId.CreateDataDogCompatibleFromDecimalString(traceId2.ToString()));
                traceId2.Should().NotBe(TraceId.CreateDataDogCompatibleFromDecimalString(traceId1.ToString()));
            }
        }

        [Fact]
        public void Equals_WorksCorrectlyBetween64And128BitIds()
        {
            var traceId1 = TraceId.CreateRandomDataDogCompatible();
            var traceId2 = TraceId.CreateFromString(TraceId.Zero.Lower.ToString("x16") + traceId1.Lower.ToString("x16"));

            using (new AssertionScope())
            {
                traceId1.Lower.Should().Be(traceId2.Lower);
                traceId1.Should().NotBe(TraceId.CreateFromString(traceId2.ToString()));
                traceId2.Should().NotBe(TraceId.CreateDataDogCompatibleFromDecimalString(traceId1.ToString()));
                traceId1.GetHashCode().Should().NotBe(traceId2.GetHashCode());
            }
        }

        [Fact]
        public void Zero_ReturnsEmpty64BitId()
        {
            var traceId = TraceId.Zero;

            using (new AssertionScope())
            {
                traceId.ToString().Should().HaveLength(1);
                traceId.ToString().Should().Be("0");
            }
        }
    }
}
