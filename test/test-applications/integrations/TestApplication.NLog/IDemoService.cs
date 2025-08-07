// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace TestApplication.NLog;

/// <summary>
/// Interface for demonstration service.
/// </summary>
public interface IDemoService
{
    Task DemonstrateServiceLoggingAsync();
}
