// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.DuckTyping;

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.RabbitMqLegacy.DuckTypes;

internal interface IBody : IDuckType
{
    public int Length { get; }
}
