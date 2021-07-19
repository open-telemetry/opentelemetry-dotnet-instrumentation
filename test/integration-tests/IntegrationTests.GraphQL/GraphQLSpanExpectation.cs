using System.Collections.Generic;
using IntegrationTests.Helpers.Mocks;
using IntegrationTests.Helpers.Models;
using OpenTelemetry.AutoInstrumentation.ClrProfiler.Managed.Tagging;

namespace IntegrationTests.GraphQL
{
    public class GraphQLSpanExpectation : WebServerSpanExpectation
    {
        public GraphQLSpanExpectation(string serviceName, string operationName, string resourceName)
            : base(serviceName, serviceVersion: null, operationName, resourceName, /* SpanTypes.GraphQL */ "GraphQL")
        {
            RegisterDelegateExpectation(ExpectErrorMatch);
            RegisterTagExpectation(nameof(Tags.GraphQL.Source), expected: GraphQLSource);
            RegisterTagExpectation(nameof(Tags.GraphQL.OperationType), expected: GraphQLOperationType);
        }

        public string GraphQLRequestBody { get; set; }

        public string GraphQLOperationType { get; set; }

        public string GraphQLOperationName { get; set; }

        public string GraphQLSource { get; set; }

        public bool IsGraphQLError { get; set; }

        private IEnumerable<string> ExpectErrorMatch(IMockSpan span)
        {
            var error = GetTag(span, Tags.ErrorMsg);
            if (string.IsNullOrEmpty(error))
            {
                if (IsGraphQLError)
                {
                    yield return $"Expected an error message but {Tags.ErrorMsg} tag is missing or empty.";
                }
            }
            else
            {
                if (!IsGraphQLError)
                {
                    yield return $"Expected no error message but {Tags.ErrorMsg} tag was {error}.";
                }
            }
        }
    }
}
