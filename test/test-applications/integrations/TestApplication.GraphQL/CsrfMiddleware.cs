// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace TestApplication.GraphQL;

/// <summary>
/// Middleware to handle CSRF protection for GraphQL GET requests.
/// GraphQL servers require the "GraphQL-Require-Preflight" header to bypass
/// CSRF checks for GET requests. This middleware adds the header to ensure
/// that GET requests are not blocked due to missing CSRF tokens.
/// </summary>
public class CsrfMiddleware
{
    private readonly RequestDelegate _next;

    public CsrfMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        context.Request.Headers["GraphQL-Require-Preflight"] = "1";

        await _next(context).ConfigureAwait(false);
    }
}
