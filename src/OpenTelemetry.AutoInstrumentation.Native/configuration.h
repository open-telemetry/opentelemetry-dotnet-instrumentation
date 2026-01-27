/*
 * Copyright The OpenTelemetry Authors
 * SPDX-License-Identifier: Apache-2.0
 */

#ifndef OTEL_CLR_CONFIGURATION_H_
#define OTEL_CLR_CONFIGURATION_H_

namespace trace
{

bool IsSqlClientNetFxILRewriteEnabled();
void SetSqlClientNetFxILRewriteEnabled(bool enabled);

} // namespace trace

#endif // OTEL_CLR_CONFIGURATION_H_