// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.AutoInstrumentation.ContinuousProfiler;

internal enum RuntimeSamplerAuthority : uint
{
    None = 0,
    Seed = 1,
    ControlPlane = 2
}
