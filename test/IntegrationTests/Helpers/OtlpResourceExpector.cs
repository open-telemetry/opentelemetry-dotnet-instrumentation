// <copyright file="OtlpResourceExpector.cs" company="OpenTelemetry Authors">
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

using System.Text;
using Google.Protobuf.Collections;
using OpenTelemetry.Proto.Common.V1;
using OpenTelemetry.Proto.Resource.V1;

namespace IntegrationTests.Helpers;

public class OtlpResourceExpector : IDisposable
{
    private readonly List<ResourceExpectation> _resourceExpectations = new();

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

    public void Expect(string key, string value)
    {
        _resourceExpectations.Add(new ResourceExpectation { Key = key, Value = value });
    }

    public void AssertExpectations(TimeSpan? timeout = null)
    {
        if (_resourceExpectations.Count == 0)
        {
            throw new InvalidOperationException("Expectations were not set");
        }

        timeout ??= Timeout.Expectation;

        try
        {
            if (!_resourceAttributesEvent.WaitOne(timeout.Value))
            {
                FailResourceMetrics(_resourceExpectations, null);
                return;
            }

            AssertResourceMetrics(_resourceExpectations, _resourceAttributes);
        }
        catch (ArgumentOutOfRangeException)
        {
            // WaitOne called with non-positive value
            FailResourceMetrics(_resourceExpectations, null);
        }
    }

    private static void AssertResourceMetrics(List<ResourceExpectation> resourceExpectations, RepeatedField<KeyValue>? actualResourceAttributes)
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

                    if (resourceAttribute.Value.StringValue != missingExpectations[i].Value)
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
            FailResourceMetrics(missingExpectations, actualResourceAttributes);
        }
    }

    private static void FailResourceMetrics(List<ResourceExpectation> missingExpectations, RepeatedField<KeyValue>? attributes)
    {
        attributes ??= new();

        var message = new StringBuilder();
        message.AppendLine();

        message.AppendLine("Missing resource expectations:");
        foreach (var expectation in missingExpectations)
        {
            message.AppendLine($"  - \"{expectation.Key}={expectation.Value}\"");
        }

        message.AppendLine("Actual resource attributes:");
        foreach (var attribute in attributes)
        {
            message.AppendLine($"  + \"{attribute.Key}={attribute.Value.StringValue}\"");
        }

        Assert.Fail(message.ToString());
    }

    private class ResourceExpectation
    {
        public string? Key { get; set; }

        public string? Value { get; set; }
    }
}
