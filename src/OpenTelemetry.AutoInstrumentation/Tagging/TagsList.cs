// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Text;

namespace OpenTelemetry.AutoInstrumentation.Tagging;

internal abstract class TagsList : ITags
{
    public List<KeyValuePair<string, string>> GetAllTags()
    {
        var additionalTags = GetAdditionalTags();
        var allTags = new List<KeyValuePair<string, string>>(
            additionalTags?.Length ?? 0);

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

    public override string ToString()
    {
        var sb = new StringBuilder();

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

    protected virtual IProperty<string?>[] GetAdditionalTags() => Array.Empty<IProperty<string?>>();
}
