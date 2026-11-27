// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Collections;
using System.Globalization;

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.Serilog.Bridge;

internal sealed class SerilogKeyValueList : IEnumerable<KeyValuePair<string, object?>>, IFormattable
{
    private readonly List<KeyValuePair<string, object?>> _entries;
    private readonly IFormattable _serilogValue;

    public SerilogKeyValueList(List<KeyValuePair<string, object?>> entries, IFormattable serilogValue)
    {
        _entries = entries;
        _serilogValue = serilogValue;
    }

    public IEnumerator<KeyValuePair<string, object?>> GetEnumerator()
    {
        return _entries.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public string ToString(string? format, IFormatProvider? formatProvider)
    {
        return _serilogValue.ToString(format, CultureInfo.InvariantCulture);
    }

    public override string ToString()
    {
        return ToString(null, null);
    }
}
