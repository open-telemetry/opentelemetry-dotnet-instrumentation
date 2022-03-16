// <copyright file="TagsList.cs" company="OpenTelemetry Authors">
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
using System.Text;
using System.Threading;

namespace OpenTelemetry.AutoInstrumentation.Tagging;

internal abstract class TagsList : ITags
{
    private List<KeyValuePair<string, string>> _tags;

    public List<KeyValuePair<string, string>> GetAllTags()
    {
        var customTags = GetCustomTags();
        var additionalTags = GetAdditionalTags();
        var allTags = new List<KeyValuePair<string, string>>(
            customTags?.Count ?? 0 +
            additionalTags?.Length ?? 0);

        if (customTags != null)
        {
            lock (customTags)
            {
                allTags.AddRange(customTags);
            }
        }

        if (additionalTags != null)
        {
            lock (additionalTags)
            {
                foreach (var property in additionalTags)
                {
                    var value = property.Getter(this);

                    if (value != null)
                    {
                        allTags.Add(new KeyValuePair<string, string>(property.Key, value));
                    }
                }
            }
        }

        return allTags;
    }

    public string GetTag(string key)
    {
        foreach (var property in GetAdditionalTags())
        {
            if (property.Key == key)
            {
                return property.Getter(this);
            }
        }

        var tags = GetCustomTags();

        if (tags == null)
        {
            return null;
        }

        lock (tags)
        {
            for (int i = 0; i < tags.Count; i++)
            {
                if (tags[i].Key == key)
                {
                    return tags[i].Value;
                }
            }
        }

        return null;
    }

    public void SetTag(string key, string value)
    {
        foreach (var property in GetAdditionalTags())
        {
            if (property.Key == key)
            {
                property.Setter(this, value);
                return;
            }
        }

        var tags = GetCustomTags();

        if (tags == null)
        {
            var newTags = new List<KeyValuePair<string, string>>();
            tags = Interlocked.CompareExchange(ref _tags, newTags, null) ?? newTags;
        }

        lock (tags)
        {
            for (int i = 0; i < tags.Count; i++)
            {
                if (tags[i].Key == key)
                {
                    if (value == null)
                    {
                        tags.RemoveAt(i);
                    }
                    else
                    {
                        tags[i] = new KeyValuePair<string, string>(key, value);
                    }

                    return;
                }
            }

            // If we get there, the tag wasn't in the collection
            if (value != null)
            {
                tags.Add(new KeyValuePair<string, string>(key, value));
            }
        }
    }

    public override string ToString()
    {
        var sb = new StringBuilder();

        var tags = GetCustomTags();

        if (tags != null)
        {
            lock (tags)
            {
                foreach (var pair in tags)
                {
                    sb.Append($"{pair.Key} (tag):{pair.Value},");
                }
            }
        }

        foreach (var property in GetAdditionalTags())
        {
            var value = property.Getter(this);

            if (value != null)
            {
                sb.Append($"{property.Key} (tag):{value},");
            }
        }

        return sb.ToString();
    }

    protected virtual IProperty<string>[] GetAdditionalTags() => Array.Empty<IProperty<string>>();

    protected virtual IList<KeyValuePair<string, string>> GetCustomTags() => Volatile.Read(ref _tags);
}
