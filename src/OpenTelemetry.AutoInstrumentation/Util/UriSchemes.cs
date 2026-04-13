// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.AutoInstrumentation.Util;

/// <summary>
/// A polyfill for Uri.UriScheme
/// </summary>
internal static class UriSchemes
{
    public static readonly string Http = Uri.UriSchemeHttp;
    public static readonly string Https = Uri.UriSchemeHttps;

#if NET
    public static readonly string Ws = Uri.UriSchemeWs;
    public static readonly string Wss = Uri.UriSchemeWss;
#else
#pragma warning disable CA1802
    public static readonly string Ws = "ws";
    public static readonly string Wss = "wss";
#pragma warning restore CA1802
#endif
}
