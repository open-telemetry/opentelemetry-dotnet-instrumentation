namespace Datadog.Trace.ClrProfiler.IntegrationTests
{
    public class WebServerSpanExpectation : SpanExpectation
    {
        public WebServerSpanExpectation(
            string serviceName,
            string serviceVersion,
            string operationName,
            string resourceName,
            string type = SpanTypes.Web,
            string statusCode = null,
            string httpMethod = null)
            : base(
                serviceName,
                serviceVersion,
                operationName,
                resourceName,
                type)
        {
            StatusCode = statusCode;
            HttpMethod = httpMethod;

            // Expectations for all spans of a web server variety should go here
            RegisterTagExpectation(Tags.HttpStatusCode, expected: StatusCode);
            RegisterTagExpectation(Tags.HttpMethod, expected: HttpMethod);
        }

        public string OriginalUri { get; set; }

        public string StatusCode { get; set; }

        public string HttpMethod { get; set; }
    }
}
