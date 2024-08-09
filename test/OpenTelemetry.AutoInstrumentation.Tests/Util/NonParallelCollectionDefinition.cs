// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using Xunit;

namespace OpenTelemetry.AutoInstrumentation.Tests.Util;

[CollectionDefinition("Non-Parallel Collection", DisableParallelization = true)]
public class NonParallelCollectionDefinition
{
}
