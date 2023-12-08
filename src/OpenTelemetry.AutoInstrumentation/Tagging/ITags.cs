// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;

namespace OpenTelemetry.AutoInstrumentation.Tagging;

internal interface ITags
{
    List<KeyValuePair<string, string>> GetAllTags();
}
