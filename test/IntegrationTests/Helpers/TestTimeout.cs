// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace IntegrationTests.Helpers;

internal static class TestTimeout
{
    public static readonly TimeSpan ProcessExit = TimeSpan.FromMinutes(5); // caution: long timeouts can cause integer overflow!
    public static readonly TimeSpan Expectation = TimeSpan.FromMinutes(1); // long to avoid flaky tests
    public static readonly TimeSpan NoExpectation = TimeSpan.FromSeconds(3); // short to not make the tests unnecessary long
}
