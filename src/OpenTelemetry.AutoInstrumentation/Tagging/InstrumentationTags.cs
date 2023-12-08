// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.AutoInstrumentation.Tagging;

internal abstract class InstrumentationTags : TagsList
{
    protected static readonly IProperty<string?>[] InstrumentationTagsProperties =
    {
    };

    protected override IProperty<string?>[] GetAdditionalTags() => InstrumentationTagsProperties;
}
