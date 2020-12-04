#if !NET45 && !NET451 && !NET452
using System;
using Datadog.Trace.ExtensionMethods;
using Xunit;

namespace Datadog.Trace.Tests
{
    public class TimeUtilsTests
    {
        [Fact]
        public void ToUnixTimeNanoseconds_UnixEpoch_Zero()
        {
            var date = DateTimeOffset.FromUnixTimeMilliseconds(0);
            Assert.Equal(0, date.ToUnixTimeNanoseconds());
        }

        [Fact]
        public void ToUnixTimeNanoseconds_Now_CorrectMillisecondRoundedValue()
        {
            var date = DateTimeOffset.UtcNow;
            Assert.Equal(date.ToUnixTimeMilliseconds(), date.ToUnixTimeNanoseconds() / 1000000);
        }

        [Fact]
        public void ToUnixTimeMicroseconds_UnixEpoch_Zero()
        {
            var date = DateTimeOffset.FromUnixTimeMilliseconds(0);
            Assert.Equal(0, date.ToUnixTimeMicroseconds());
        }

        [Fact]
        public void ToUnixTimeMicrooseconds_Now_CorrectMillisecondRoundedValue()
        {
            var date = DateTimeOffset.UtcNow;
            Assert.Equal(date.ToUnixTimeMilliseconds(), date.ToUnixTimeMicroseconds() / 1000);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(0.5)]
        [InlineData(0.001)]
        [InlineData(1500)]
        public void ToMicroseconds(double milliseconds)
        {
            var expectedTimeSpan = TimeSpan.FromMilliseconds(milliseconds);
            var microsecondsSpans = expectedTimeSpan.ToMicroseconds();
            var actualTimeSpan = TimeSpan.FromMilliseconds(microsecondsSpans / 1000.0);
            Assert.Equal(expectedTimeSpan, actualTimeSpan);
        }
    }
}
#endif
