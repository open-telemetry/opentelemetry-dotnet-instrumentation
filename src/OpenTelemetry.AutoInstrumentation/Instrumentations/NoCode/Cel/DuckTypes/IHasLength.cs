// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.NoCode.Cel.DuckTypes;

/// <summary>
/// Duck type interface for arrays and strings with Length property.
/// </summary>
internal interface IHasLength
{
    int Length { get; }
}
