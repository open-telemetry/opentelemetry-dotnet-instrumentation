// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Collections;
using System.Data.Common;

namespace TestApplication.AdoNetStub;

internal sealed class StubDbDataReader : DbDataReader
{
    public override int Depth => 0;

    public override int FieldCount => 0;

    public override bool HasRows => false;

    public override bool IsClosed => false;

    public override int RecordsAffected => 0;

    public override object this[int ordinal] => string.Empty;

    public override object this[string name] => string.Empty;

    public override bool GetBoolean(int ordinal) => false;

    public override byte GetByte(int ordinal) => 0;

    public override long GetBytes(int ordinal, long dataOffset, byte[]? buffer, int bufferOffset, int length) => 0;

    public override char GetChar(int ordinal) => '\0';

    public override long GetChars(int ordinal, long dataOffset, char[]? buffer, int bufferOffset, int length) => 0;

    public override string GetDataTypeName(int ordinal) => "string";

    public override DateTime GetDateTime(int ordinal) => DateTime.MinValue;

    public override decimal GetDecimal(int ordinal) => 0;

    public override double GetDouble(int ordinal) => 0;

    public override IEnumerator GetEnumerator() => Array.Empty<object>().GetEnumerator();

    public override Type GetFieldType(int ordinal) => typeof(string);

    public override float GetFloat(int ordinal) => 0;

    public override Guid GetGuid(int ordinal) => Guid.Empty;

    public override short GetInt16(int ordinal) => 0;

    public override int GetInt32(int ordinal) => 0;

    public override long GetInt64(int ordinal) => 0;

    public override string GetName(int ordinal) => "Column";

    public override int GetOrdinal(string name) => 0;

    public override string GetString(int ordinal) => string.Empty;

    public override object GetValue(int ordinal) => string.Empty;

    public override int GetValues(object[] values) => 0;

    public override bool IsDBNull(int ordinal) => false;

    public override bool NextResult() => false;

    public override bool Read() => false;
}
