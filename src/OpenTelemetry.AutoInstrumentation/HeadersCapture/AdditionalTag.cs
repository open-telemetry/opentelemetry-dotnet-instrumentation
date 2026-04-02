// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.AutoInstrumentation.HeadersCapture;

internal class AdditionalTag
{
    private AdditionalTag(string key, string spanTagPrefix)
    {
        Key = key;
        TagName = $"{spanTagPrefix}.{HeaderNormalizer.Normalize(key)}";
    }

    public string Key { get; }

    public string TagName { get; }

    public static AdditionalTag CreateGrpcRequestCache(string key)
    {
        return new AdditionalTag(key, Constants.GrpcSpanAttributes.AttributeGrpcRequestMetadataPrefix);
    }

    public static AdditionalTag CreateGrpcResponseCache(string key)
    {
        return new AdditionalTag(key, Constants.GrpcSpanAttributes.AttributeGrpcResponseMetadataPrefix);
    }

    public static AdditionalTag CreateHttpRequestCache(string key)
    {
        return new AdditionalTag(key, Constants.HttpSpanAttributes.AttributeHttpRequestHeaderPrefix);
    }

    public static AdditionalTag CreateHttpResponseCache(string key)
    {
        return new AdditionalTag(key, Constants.HttpSpanAttributes.AttributeHttpResponseHeaderPrefix);
    }

    public override bool Equals(object? obj)
    {
        if (obj is AdditionalTag other)
        {
            return Key == other.Key && TagName == other.TagName;
        }

        return false;
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = 17;
#if NET
            hash = (hash * 23) + (Key != null ? Key.GetHashCode(StringComparison.Ordinal) : 0);
            hash = (hash * 23) + (TagName != null ? TagName.GetHashCode(StringComparison.Ordinal) : 0);
#else
            hash = (hash * 23) + (Key != null ? Key.GetHashCode() : 0);
            hash = (hash * 23) + (TagName != null ? TagName.GetHashCode() : 0);
#endif
            return hash;
        }
    }
}
