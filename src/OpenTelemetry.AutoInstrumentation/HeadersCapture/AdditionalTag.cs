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
}
