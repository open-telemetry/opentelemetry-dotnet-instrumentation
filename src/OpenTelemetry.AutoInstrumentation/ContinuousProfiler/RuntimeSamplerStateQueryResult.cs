// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.AutoInstrumentation.ContinuousProfiler;

internal enum RuntimeSamplerStateQueryResult
{
    Succeeded = 0,
    InvalidArgument = 1,
    UnsupportedLayout = 2
}
