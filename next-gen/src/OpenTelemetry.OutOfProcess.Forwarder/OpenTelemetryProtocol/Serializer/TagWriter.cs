// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Globalization;

namespace OpenTelemetry.Internal;

internal abstract class TagWriter<TTagState, TArrayState>
    where TTagState : notnull
    where TArrayState : notnull
{
    private static ReadOnlySpan<char> TruncateString(ReadOnlySpan<char> value, int? maxLength)
    {
        return maxLength.HasValue && value.Length > maxLength
            ? value.Slice(0, maxLength.Value)
            : value;
    }

    private readonly ArrayTagWriter<TArrayState> _ArrayWriter;

    protected TagWriter(
        ArrayTagWriter<TArrayState> arrayTagWriter)
    {
        // Guard.ThrowIfNull(arrayTagWriter);
        _ArrayWriter = arrayTagWriter;
    }

    public bool TryWriteTag(
        ref TTagState state,
        KeyValuePair<string, object?> tag,
        int? tagValueMaxLength = null)
    {
        return TryWriteTag(ref state, tag.Key, tag.Value, tagValueMaxLength);
    }

    public bool TryWriteTag(
        ref TTagState state,
        string key,
        object? value,
        int? tagValueMaxLength = null)
    {
        if (value == null)
        {
            return TryWriteEmptyTag(ref state, key, value);
        }

        switch (value)
        {
            case char c:
                WriteCharTag(ref state, key, c);
                break;
            case string s:
                WriteStringTag(
                    ref state,
                    key,
                    TruncateString(s.AsSpan(), tagValueMaxLength));
                break;
            case bool b:
                WriteBooleanTag(ref state, key, b);
                break;
            case byte:
            case sbyte:
            case short:
            case ushort:
            case int:
            case uint:
            case long:
                WriteIntegralTag(ref state, key, Convert.ToInt64(value, CultureInfo.InvariantCulture));
                break;
            case float:
            case double:
                WriteFloatingPointTag(ref state, key, Convert.ToDouble(value, CultureInfo.InvariantCulture));
                break;
            case Array array:
                try
                {
                    WriteArrayTagInternal(ref state, key, array, tagValueMaxLength);
                }
                catch (Exception ex) when (ex is IndexOutOfRangeException || ex is ArgumentException)
                {
                    throw;
                }
                catch
                {
                    // If an exception is thrown when calling ToString
                    // on any element of the array, then the entire array value
                    // is ignored.
                    return LogUnsupportedTagTypeAndReturnFalse(key, value);
                }

                break;

            // All other types are converted to strings including the following
            // built-in value types:
            // case nint:    Pointer type.
            // case nuint:   Pointer type.
            // case ulong:   May throw an exception on overflow.
            // case decimal: Converting to double produces rounding errors.
            default:
                try
                {
                    string? stringValue = Convert.ToString(value, CultureInfo.InvariantCulture);
                    if (stringValue == null)
                    {
                        return LogUnsupportedTagTypeAndReturnFalse(key, value);
                    }

                    WriteStringTag(
                        ref state,
                        key,
                        TruncateString(stringValue.AsSpan(), tagValueMaxLength));
                }
                catch
                {
                    // If ToString throws an exception then the tag is ignored.
                    return LogUnsupportedTagTypeAndReturnFalse(key, value);
                }

                break;
        }

        return true;
    }

    protected abstract bool TryWriteEmptyTag(ref TTagState state, string key, object? value);

    protected abstract void WriteIntegralTag(ref TTagState state, string key, long value);

    protected abstract void WriteFloatingPointTag(ref TTagState state, string key, double value);

    protected abstract void WriteBooleanTag(ref TTagState state, string key, bool value);

    protected abstract void WriteStringTag(ref TTagState state, string key, ReadOnlySpan<char> value);

    protected abstract void WriteArrayTag(ref TTagState state, string key, ref TArrayState value);

    protected abstract void OnUnsupportedTagDropped(
        string tagKey,
        string tagValueTypeFullName);

    private void WriteCharTag(ref TTagState state, string key, char value)
    {
        Span<char> destination = [value];
        WriteStringTag(ref state, key, destination);
    }

    private void WriteCharValue(ref TArrayState state, char value)
    {
        Span<char> destination = [value];
        _ArrayWriter.WriteStringValue(ref state, destination);
    }

    private void WriteArrayTagInternal(ref TTagState state, string key, Array array, int? tagValueMaxLength)
    {
        TArrayState arrayState = _ArrayWriter.BeginWriteArray();

        try
        {
            // This switch ensures the values of the resultant array-valued tag are of the same type.
            switch (array)
            {
                case char[] charArray: WriteStructToArray(ref arrayState, charArray); break;
                case string?[] stringArray: WriteStringsToArray(ref arrayState, stringArray, tagValueMaxLength); break;
                case bool[] boolArray: WriteStructToArray(ref arrayState, boolArray); break;
                case byte[] byteArray: WriteToArrayCovariant(ref arrayState, byteArray); break;
                case short[] shortArray: WriteToArrayCovariant(ref arrayState, shortArray); break;
#if NETFRAMEWORK
                case int[]: this.WriteArrayTagIntNetFramework(ref arrayState, array, tagValueMaxLength); break;
                case long[]: this.WriteArrayTagLongNetFramework(ref arrayState, array, tagValueMaxLength); break;
#else
                case int[] intArray: WriteToArrayCovariant(ref arrayState, intArray); break;
                case long[] longArray: WriteToArrayCovariant(ref arrayState, longArray); break;
#endif
                case float[] floatArray: WriteStructToArray(ref arrayState, floatArray); break;
                case double[] doubleArray: WriteStructToArray(ref arrayState, doubleArray); break;
                default: WriteToArrayTypeChecked(ref arrayState, array, tagValueMaxLength); break;
            }

            _ArrayWriter.EndWriteArray(ref arrayState);
        }
        catch (Exception ex) when (ex is IndexOutOfRangeException || ex is ArgumentException)
        {
            // If the array writer cannot be resized, TryResize should log a message to the event source, return false.
            if (_ArrayWriter.TryResize())
            {
                WriteArrayTagInternal(ref state, key, array, tagValueMaxLength);
                return;
            }

            // Drop the array value and set "TRUNCATED" as value for easier isolation.
            // This is a best effort to avoid dropping the entire tag.
            WriteStringTag(
                ref state,
                key,
                "TRUNCATED".AsSpan());

            LogUnsupportedTagTypeAndReturnFalse(key, array.GetType().ToString());
            return;
        }

        WriteArrayTag(ref state, key, ref arrayState);
    }

    private void WriteToArrayTypeChecked(ref TArrayState arrayState, Array array, int? tagValueMaxLength)
    {
        for (int i = 0; i < array.Length; ++i)
        {
            object? item = array.GetValue(i);
            if (item == null)
            {
                _ArrayWriter.WriteNullValue(ref arrayState);
                continue;
            }

            switch (item)
            {
                case char c:
                    WriteCharValue(ref arrayState, c);
                    break;
                case string s:
                    _ArrayWriter.WriteStringValue(
                        ref arrayState,
                        TruncateString(s.AsSpan(), tagValueMaxLength));
                    break;
                case bool b:
                    _ArrayWriter.WriteBooleanValue(ref arrayState, b);
                    break;
                case byte:
                case sbyte:
                case short:
                case ushort:
                case int:
                case uint:
                case long:
                    _ArrayWriter.WriteIntegralValue(ref arrayState, Convert.ToInt64(item, CultureInfo.InvariantCulture));
                    break;
                case float:
                case double:
                    _ArrayWriter.WriteFloatingPointValue(ref arrayState, Convert.ToDouble(item, CultureInfo.InvariantCulture));
                    break;

                // All other types are converted to strings including the following
                // built-in value types:
                // case Array:   Nested array.
                // case nint:    Pointer type.
                // case nuint:   Pointer type.
                // case ulong:   May throw an exception on overflow.
                // case decimal: Converting to double produces rounding errors.
                default:
                    string? stringValue = Convert.ToString(item, CultureInfo.InvariantCulture);
                    if (stringValue == null)
                    {
                        _ArrayWriter.WriteNullValue(ref arrayState);
                    }
                    else
                    {
                        _ArrayWriter.WriteStringValue(
                            ref arrayState,
                            TruncateString(stringValue.AsSpan(), tagValueMaxLength));
                    }

                    break;
            }
        }
    }

    private void WriteToArrayCovariant<TItem>(ref TArrayState arrayState, TItem[] array)
        where TItem : struct
    {
        // Note: The runtime treats int[]/uint[], byte[]/sbyte[],
        // short[]/ushort[], and long[]/ulong[] as covariant.
        if (typeof(TItem) == typeof(byte))
        {
            if (array.GetType() == typeof(sbyte[]))
            {
                WriteStructToArray(ref arrayState, (sbyte[])(object)array);
            }
            else
            {
                WriteStructToArray(ref arrayState, (byte[])(object)array);
            }
        }
        else if (typeof(TItem) == typeof(short))
        {
            if (array.GetType() == typeof(ushort[]))
            {
                WriteStructToArray(ref arrayState, (ushort[])(object)array);
            }
            else
            {
                WriteStructToArray(ref arrayState, (short[])(object)array);
            }
        }
        else if (typeof(TItem) == typeof(int))
        {
            if (array.GetType() == typeof(uint[]))
            {
                WriteStructToArray(ref arrayState, (uint[])(object)array);
            }
            else
            {
                WriteStructToArray(ref arrayState, (int[])(object)array);
            }
        }
        else if (typeof(TItem) == typeof(long))
        {
            if (array.GetType() == typeof(ulong[]))
            {
                WriteToArrayTypeChecked(ref arrayState, array, tagValueMaxLength: null);
            }
            else
            {
                WriteStructToArray(ref arrayState, (long[])(object)array);
            }
        }
        else
        {
            Debug.Fail("Unexpected type encountered");

            throw new NotSupportedException();
        }
    }

    private void WriteStructToArray<TItem>(ref TArrayState arrayState, TItem[] array)
        where TItem : struct
    {
        foreach (TItem item in array)
        {
            if (typeof(TItem) == typeof(char))
            {
                WriteCharValue(ref arrayState, (char)(object)item);
            }
            else if (typeof(TItem) == typeof(bool))
            {
                _ArrayWriter.WriteBooleanValue(ref arrayState, (bool)(object)item);
            }
            else if (typeof(TItem) == typeof(byte))
            {
                _ArrayWriter.WriteIntegralValue(ref arrayState, (byte)(object)item);
            }
            else if (typeof(TItem) == typeof(sbyte))
            {
                _ArrayWriter.WriteIntegralValue(ref arrayState, (sbyte)(object)item);
            }
            else if (typeof(TItem) == typeof(short))
            {
                _ArrayWriter.WriteIntegralValue(ref arrayState, (short)(object)item);
            }
            else if (typeof(TItem) == typeof(ushort))
            {
                _ArrayWriter.WriteIntegralValue(ref arrayState, (ushort)(object)item);
            }
            else if (typeof(TItem) == typeof(int))
            {
                _ArrayWriter.WriteIntegralValue(ref arrayState, (int)(object)item);
            }
            else if (typeof(TItem) == typeof(uint))
            {
                _ArrayWriter.WriteIntegralValue(ref arrayState, (uint)(object)item);
            }
            else if (typeof(TItem) == typeof(long))
            {
                _ArrayWriter.WriteIntegralValue(ref arrayState, (long)(object)item);
            }
            else if (typeof(TItem) == typeof(float))
            {
                _ArrayWriter.WriteFloatingPointValue(ref arrayState, (float)(object)item);
            }
            else if (typeof(TItem) == typeof(double))
            {
                _ArrayWriter.WriteFloatingPointValue(ref arrayState, (double)(object)item);
            }
            else
            {
                Debug.Fail("Unexpected type encountered");

                throw new NotSupportedException();
            }
        }
    }

    private void WriteStringsToArray(ref TArrayState arrayState, string?[] array, int? tagValueMaxLength)
    {
        foreach (string? item in array)
        {
            if (item == null)
            {
                _ArrayWriter.WriteNullValue(ref arrayState);
            }
            else
            {
                _ArrayWriter.WriteStringValue(
                    ref arrayState,
                    TruncateString(item.AsSpan(), tagValueMaxLength));
            }
        }
    }

    private bool LogUnsupportedTagTypeAndReturnFalse(string key, object value)
    {
        Debug.Assert(value != null, "value was null");

        OnUnsupportedTagDropped(key, value!.GetType().ToString());
        return false;
    }
}
