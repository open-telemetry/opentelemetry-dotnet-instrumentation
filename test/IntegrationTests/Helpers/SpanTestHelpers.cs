// <copyright file="SpanTestHelpers.cs" company="OpenTelemetry Authors">
// Copyright The OpenTelemetry Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using IntegrationTests.Helpers.Mocks;
using IntegrationTests.Helpers.Models;
using Xunit;

namespace IntegrationTests.Helpers;

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
