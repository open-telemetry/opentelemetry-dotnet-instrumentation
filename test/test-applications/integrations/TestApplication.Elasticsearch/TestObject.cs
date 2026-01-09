// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace TestApplication.Elasticsearch;

#pragma warning disable CA1812 // Avoid uninstantiated internal classes. This class is instantiated by reflection.
internal sealed class TestObject
#pragma warning restore CA1812 // Avoid uninstantiated internal classes. This class is instantiated by reflection.
{
    public int Id { get; set; }
}
