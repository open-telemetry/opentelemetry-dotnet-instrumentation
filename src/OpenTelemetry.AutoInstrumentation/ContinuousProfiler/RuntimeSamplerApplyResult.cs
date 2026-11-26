// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.AutoInstrumentation.ContinuousProfiler;

internal enum RuntimeSamplerApplyResult
{
    Applied = 0,
    NoChange = 1,
    IgnoredSeedAlreadyCommitted = 2,
    IgnoredLowerAuthority = 3,
    RejectedInvalidArgument = 4,
    RejectedUnsupportedLayout = 5,
    RejectedInvalidConfiguration = 6,
    RejectedUnsupportedRuntime = 7,
    ActivationFailed = 8,
    ShuttingDown = 9
}
