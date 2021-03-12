// taken from: https://github.com/open-telemetry/opentelemetry-dotnet/blob/main/test/OpenTelemetry.Tests/Trace/StatusTest.cs
using Xunit;

namespace Datadog.Trace.Tests
{
    public class SpanStatusTests
    {
        [Fact]
        public void Status_Ok()
        {
            Assert.Equal(StatusCode.Ok, SpanStatus.Ok.StatusCode);
            Assert.Null(SpanStatus.Ok.Description);
        }

        [Fact]
        public void CheckingDefaultStatus()
        {
            Assert.Equal(default, SpanStatus.Unset);
        }

        [Fact]
        public void CreateStatus_Error_WithDescription()
        {
            var status = SpanStatus.Error.WithDescription("This is an error.");
            Assert.Equal(StatusCode.Error, status.StatusCode);
            Assert.Equal("This is an error.", status.Description);
        }

        [Fact]
        public void CreateStatus_Ok_WithDescription()
        {
            var status = SpanStatus.Ok.WithDescription("This is will not be set.");
            Assert.Equal(StatusCode.Ok, status.StatusCode);
            Assert.Null(status.Description);
        }

        [Fact]
        public void Equality()
        {
            var status1 = new SpanStatus(StatusCode.Ok);
            var status2 = new SpanStatus(StatusCode.Ok);
            object status3 = new SpanStatus(StatusCode.Ok);

            Assert.Equal(status1, status2);
            Assert.True(status1 == status2);
            Assert.True(status1.Equals(status3));
        }

        [Fact]
        public void Equality_WithDescription()
        {
            var status1 = new SpanStatus(StatusCode.Error, "error");
            var status2 = new SpanStatus(StatusCode.Error, "error");

            Assert.Equal(status1, status2);
            Assert.True(status1 == status2);
        }

        [Fact]
        public void Not_Equality()
        {
            var status1 = new SpanStatus(StatusCode.Ok);
            var status2 = new SpanStatus(StatusCode.Error);
            object notStatus = 1;

            Assert.NotEqual(status1, status2);
            Assert.True(status1 != status2);
            Assert.False(status1.Equals(notStatus));
        }

        [Fact]
        public void Not_Equality_WithDescription1()
        {
            var status1 = new SpanStatus(StatusCode.Ok, "ok");
            var status2 = new SpanStatus(StatusCode.Error, "error");

            Assert.NotEqual(status1, status2);
            Assert.True(status1 != status2);
        }

        [Fact]
        public void Not_Equality_WithDescription2()
        {
            var status1 = new SpanStatus(StatusCode.Ok);
            var status2 = new SpanStatus(StatusCode.Error, "error");

            Assert.NotEqual(status1, status2);
            Assert.True(status1 != status2);
        }

        [Fact]
        public void TestToString()
        {
            var status = new SpanStatus(StatusCode.Ok);
            Assert.Equal($"SpanStatus{{StatusCode={status.StatusCode}, Description={status.Description}}}", status.ToString());
        }

        [Fact]
        public void TestGetHashCode()
        {
            var status = new SpanStatus(StatusCode.Ok);
            Assert.NotEqual(0, status.GetHashCode());
        }
    }
}
