// <copyright file="DictionaryHelpers.cs" company="OpenTelemetry Authors">
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

namespace IntegrationTests.Helpers;

internal static class DictionaryHelpers
{
    public static ICollection<(string Key, string Value)> ToEnvironmentVariablesList(this IEnumerable<string> list)
    {
        return list.Select(x =>
            {
                var keyValuePair = x.Split(new[] { '=' }, 2);

                return (keyValuePair[0], keyValuePair[1]);
            })
            .ToList();
    }
}