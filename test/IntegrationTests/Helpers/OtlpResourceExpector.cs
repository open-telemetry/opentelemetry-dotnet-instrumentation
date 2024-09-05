// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Globalization;
using System.Text;
using Google.Protobuf.Collections;
using OpenTelemetry.Proto.Common.V1;
using OpenTelemetry.Proto.Resource.V1;

namespace IntegrationTests.Helpers;

public class OtlpResourceExpector : IDisposable
{
    private readonly List<ResourceExpectation> _resourceExpectations = new();
    private readonly List<string> _existenceChecks = new();

    private readonly ManualResetEvent _resourceAttributesEvent = new(false); // synchronizes access to _resourceAttributes
    private RepeatedField<KeyValue>? _resourceAttributes; // protobuf type

    public void Dispose()
    {
        _resourceAttributesEvent.Dispose();
    }

    public void Collect(Resource resource)
    {
        // resource metrics are always the same. set them only once.
        if (_resourceAttributes == null)
        {
            _resourceAttributes = resource.Attributes;
            _resourceAttributesEvent.Set();
        }
    }

    public void Exist(string key)
    {
        _existenceChecks.Add(key);
    }

    public void Expect(string key, string value)
    {
        _resourceExpectations.Add(new ResourceExpectation(key, value));
    }

    public void Expect(string key, long value)
    {
        _resourceExpectations.Add(new ResourceExpectation(key, value));
    }

    public void AssertExpectations(TimeSpan? timeout = null)
    {
        if (_resourceExpectations.Count == 0 && _existenceChecks.Count == 0)
        {
            throw new InvalidOperationException("Expectations were not set");
        }

        timeout ??= TestTimeout.Expectation;

        try
        {
            if (!_resourceAttributesEvent.WaitOne(timeout.Value))
            {
                FailResource(_resourceExpectations, null);
                return;
            }

            if (_resourceAttributes == null)
            {
                Assert.Fail("No resource attributes have been collected");
            }

            var message = new StringBuilder();

            foreach (var key in _existenceChecks)
            {
                var keyExists = _resourceAttributes.Any(attr => attr.Key == key);
                if (!keyExists)
                {
                    message.AppendLine($"Resource attribute \"{key}\" was not found");
                }
            }

            if (message.Length > 0)
            {
                Assert.Fail(message.ToString());
            }

            AssertResource(_resourceExpectations, _resourceAttributes);
        }
        catch (ArgumentOutOfRangeException)
        {
            // WaitOne called with non-positive value
            FailResource(_resourceExpectations, null);
        }
    }

    private static void AssertResource(List<ResourceExpectation> resourceExpectations, RepeatedField<KeyValue>? actualResourceAttributes)
    {
        var missingExpectations = new List<ResourceExpectation>(resourceExpectations);
        if (actualResourceAttributes != null)
        {
            foreach (var resourceAttribute in actualResourceAttributes)
            {
                for (var i = missingExpectations.Count - 1; i >= 0; i--)
                {
                    if (resourceAttribute.Key != missingExpectations[i].Key)
                    {
                        continue;
                    }

                    if (missingExpectations[i].StringValue != null && resourceAttribute.Value.StringValue != missingExpectations[i].StringValue)
                    {
                        continue;
                    }

                    if (missingExpectations[i].IntValue != null && resourceAttribute.Value.IntValue != missingExpectations[i].IntValue)
                    {
                        continue;
                    }

                    missingExpectations.RemoveAt(i);
                    break;
                }
            }
        }

        if (missingExpectations.Count > 0)
        {
            FailResource(missingExpectations, actualResourceAttributes);
        }
    }

    private static void FailResource(List<ResourceExpectation> missingExpectations, RepeatedField<KeyValue>? attributes)
    {
        attributes ??= new();

        var message = new StringBuilder();
        message.AppendLine();

        message.AppendLine("Missing resource expectations:");
        foreach (var expectation in missingExpectations)
        {
            var value = !string.IsNullOrEmpty(expectation.StringValue) ? expectation.StringValue : expectation.IntValue!.Value.ToString(CultureInfo.InvariantCulture);
            message.AppendLine($"  - \"{expectation.Key}={value}\"");
        }

        message.AppendLine("Actual resource attributes:");
        foreach (var attribute in attributes)
        {
            var value = !string.IsNullOrEmpty(attribute.Value.StringValue) ? attribute.Value.StringValue : attribute.Value.IntValue.ToString(CultureInfo.InvariantCulture);
            message.AppendLine($"  + \"{attribute.Key}={value}\"");
        }

        Assert.Fail(message.ToString());
    }

    private class ResourceExpectation
    {
        public ResourceExpectation(string key, string stringValue)
        {
            Key = key;
            StringValue = stringValue;
        }

        public ResourceExpectation(string key, long intValue)
        {
            Key = key;
            IntValue = intValue;
        }

        public string Key { get; }

        public string? StringValue { get; }

        public long? IntValue { get; }
    }
}
