// <copyright file="SpanExpectation.cs" company="OpenTelemetry Authors">
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
using OpenTelemetry.AutoInstrumentation.Tagging;

namespace IntegrationTests.Helpers.Models;

/// <summary>
/// Base class for all span expectations. Inherit from this class to extend per type or integration.
/// </summary>
public class SpanExpectation
{
    public SpanExpectation(string serviceName, string serviceVersion, string operationName, string library)
    {
        ServiceName = serviceName;
        ServiceVersion = serviceVersion;
        OperationName = operationName;
        Library = library;

        // Expectations for all spans regardless of type should go here
        RegisterDelegateExpectation(ExpectBasicSpanDataExists);

        RegisterCustomExpectation(nameof(OperationName), actual: s => s.Name, expected: OperationName);
        RegisterCustomExpectation(nameof(ServiceName), actual: s => s.Service, expected: ServiceName);
        RegisterCustomExpectation(nameof(Library), actual: s => s.Library, expected: Library);

        RegisterTagExpectation(
            key: "otel.library.version",
            expected: ServiceVersion);
    }

    public Func<IMockSpan, bool> Always => s => true;

    public List<Func<IMockSpan, string>> Assertions { get; } = new List<Func<IMockSpan, string>>();

    public bool IsTopLevel { get; set; } = true;

    public string Library { get; set; }

    public string OperationName { get; set; }

    public string ServiceName { get; set; }

    public string ServiceVersion { get; set; }

    public static string GetTag(IMockSpan span, string tag)
    {
        span.Tags.TryGetValue(tag, out var value);
        return value;
    }

    public override string ToString()
    {
        return $"service={ServiceName}, operation={OperationName}, library={Library}, version={ServiceVersion}";
    }

    /// <summary>
    /// Override for custom filters.
    /// </summary>
    /// <param name="span">The span on which to filter.</param>
    /// <returns>Whether the span qualifies for this expectation.</returns>
    public virtual bool Matches(IMockSpan span)
    {
        return span.Service == ServiceName
               && span.Name == OperationName
               && span.Library == Library;
    }

    /// <summary>
    /// The aggregate assertion which is run for a test.
    /// </summary>
    /// <param name="span">The span being asserted against.</param>
    /// <param name="message">The developer friendly message for the test failure.</param>
    /// <returns>Whether the span meets expectations.</returns>
    public bool MeetsExpectations(IMockSpan span, out string message)
    {
        message = string.Empty;

        var messages = new List<string>();

        foreach (var assertion in Assertions)
        {
            var mismatchMessage = assertion(span);
            if (!string.IsNullOrWhiteSpace(mismatchMessage))
            {
                messages.Add(mismatchMessage);
            }
        }

        if (messages.Any())
        {
            message = string.Join(",", messages);
            return false;
        }

        return true;
    }

    public void TagShouldExist(string tagKey, Func<IMockSpan, bool> when)
    {
        Assertions.Add(span =>
        {
            if (when(span) && !span.Tags.ContainsKey(tagKey))
            {
                return $"Tag {tagKey} is missing from span.";
            }

            return null;
        });
    }

    public void RegisterDelegateExpectation(Func<IMockSpan, IEnumerable<string>> expectation)
    {
        if (expectation == null)
        {
            return;
        }

        Assertions.Add(span =>
        {
            var failures = expectation(span)?.ToArray();

            if (failures != null && failures.Any())
            {
                return string.Join(",", failures);
            }

            return null;
        });
    }

    public void RegisterCustomExpectation(
        string keyForMessage,
        Func<IMockSpan, string> actual,
        string expected)
    {
        Assertions.Add(span =>
        {
            var actualValue = actual(span);

            if (expected != null && actualValue != expected)
            {
                return FailureMessage(name: keyForMessage, actual: actualValue, expected: expected);
            }

            return null;
        });
    }

    public void RegisterTagExpectation(
        string key,
        string expected,
        Func<IMockSpan, bool> when = null)
    {
        when ??= Always;

        Assertions.Add(span =>
        {
            if (!when(span))
            {
                return null;
            }

            var actualValue = GetTag(span, key);

            if (actualValue != expected)
            {
                return FailureMessage(name: key, actual: actualValue, expected: expected);
            }

            return null;
        });
    }

    protected string FailureMessage(string name, string actual, string expected)
    {
        return $"({name} mismatch: actual: {actual ?? "NULL"}, expected: {expected ?? "NULL"})";
    }

    private IEnumerable<string> ExpectBasicSpanDataExists(IMockSpan span)
    {
        if (string.IsNullOrWhiteSpace(span.Library))
        {
            yield return "Library must be set.";
        }

        if (string.IsNullOrWhiteSpace(span.Name))
        {
            yield return "Name must be set.";
        }

        if (string.IsNullOrWhiteSpace(span.Service))
        {
            yield return "Service must be set.";
        }

        if (span.TraceId == default)
        {
            yield return "TraceId must be set.";
        }

        if (span.SpanId == default)
        {
            yield return "SpanId must be set.";
        }
    }
}
