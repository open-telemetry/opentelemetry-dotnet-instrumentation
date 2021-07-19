using System;
using System.Collections.Generic;
using System.Linq;
using IntegrationTests.Helpers.Mocks;
using IntegrationTests.Helpers.Models;
using Xunit;

namespace IntegrationTests.Helpers
{
    public class SpanTestHelpers
    {
        public static void AssertExpectationsMet<T>(
            List<T> expectations,
            List<IMockSpan> spans)
            where T : SpanExpectation
        {
            Assert.True(spans.Count >= expectations.Count, $"Expected at least {expectations.Count} spans, received {spans.Count}");

            List<string> failures = new List<string>();
            List<IMockSpan> remainingSpans = spans.Select(s => s).ToList();

            foreach (SpanExpectation expectation in expectations)
            {
                List<IMockSpan> possibleSpans =
                    remainingSpans
                       .Where(s => expectation.Matches(s))
                       .ToList();

                if (possibleSpans.Count == 0)
                {
                    failures.Add($"No spans for: {expectation}");
                    continue;
                }

                IMockSpan resultSpan = possibleSpans.First();

                if (!remainingSpans.Remove(resultSpan))
                {
                    throw new Exception("Failed to remove an inspected span, can't trust this test.'");
                }

                if (!expectation.MeetsExpectations(resultSpan, out var failureMessage))
                {
                    failures.Add($"{expectation} failed with: {failureMessage}");
                }
            }

            string finalMessage = Environment.NewLine + string.Join(Environment.NewLine, failures.Select(f => " - " + f));

            Assert.True(!failures.Any(), finalMessage);
            Assert.True(remainingSpans.Count == 0, $"There were {remainingSpans.Count} spans unaccounted for.");
        }
    }
}
