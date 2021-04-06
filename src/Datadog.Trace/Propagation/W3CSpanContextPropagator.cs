using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Datadog.Trace.Conventions;
using Datadog.Trace.Logging;

namespace Datadog.Trace.Propagation
{
    internal class W3CSpanContextPropagator : IPropagator
    {
        private const string TraceParentFormat = "00-{0}-{1}-01";

        private static readonly IDatadogLogger Log = DatadogLogging.GetLoggerFor<W3CSpanContextPropagator>();
        private static readonly int VersionPrefixIdLength = "00-".Length;
        private static readonly int TraceIdLength = "0af7651916cd43dd8448eb211c80319c".Length;
        private static readonly int VersionAndTraceIdLength = "00-0af7651916cd43dd8448eb211c80319c-".Length;
        private static readonly int SpanIdLength = "00f067aa0ba902b7".Length;

        // The following length limits are from Trace Context v1 https://www.w3.org/TR/trace-context-1/#key
        private static readonly int TraceStateKeyMaxLength = 256;
        private static readonly int TraceStateKeyTenantMaxLength = 241;
        private static readonly int TraceStateKeyVendorMaxLength = 14;
        private static readonly int TraceStateValueMaxLength = 256;

        private readonly ITraceIdConvention _traceIdConvention;

        public W3CSpanContextPropagator(ITraceIdConvention traceIdConvention)
        {
            _traceIdConvention = traceIdConvention;
        }

        public void Inject<T>(SpanContext context, T carrier, Action<T, string, string> setter)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (carrier == null)
            {
                throw new ArgumentNullException(nameof(carrier));
            }

            if (setter == null)
            {
                throw new ArgumentNullException(nameof(setter));
            }

            // lock sampling priority when span propagates.
            context.TraceContext?.LockSamplingPriority();

            setter(carrier, W3CHeaderNames.TraceParent, string.Format(TraceParentFormat, context.TraceId.ToString(), context.SpanId.ToString("x16")));
            if (!string.IsNullOrEmpty(context.TraceState))
            {
                setter(carrier, W3CHeaderNames.TraceState, context.TraceState);
            }
        }

        public SpanContext Extract<T>(T carrier, Func<T, string, IEnumerable<string>> getter)
        {
            if (carrier == null)
            {
                throw new ArgumentNullException(nameof(carrier));
            }

            if (getter == null)
            {
                throw new ArgumentNullException(nameof(getter));
            }

            var traceStateCollection = getter(carrier, W3CHeaderNames.TraceState);
            var traceState = ExtractTraceState(traceStateCollection);

            var traceParentValues = getter(carrier, W3CHeaderNames.TraceParent).ToList();
            if (traceParentValues.Count != 1)
            {
                Log.Warning("Header {HeaderName} needs exactly 1 value", W3CHeaderNames.TraceParent);
                return null;
            }

            var traceParentHeader = traceParentValues[index: 0];
            var traceIdString = traceParentHeader.Substring(VersionPrefixIdLength, TraceIdLength);
            var traceId = _traceIdConvention.CreateFromString(traceIdString);
            if (traceId == TraceId.Zero)
            {
                Log.Warning("Could not parse {HeaderName} headers: {HeaderValues}", W3CHeaderNames.TraceParent, string.Join(",", traceParentValues));
                return null;
            }

            var spanIdString = traceParentHeader.Substring(VersionAndTraceIdLength, SpanIdLength);
            if (ulong.TryParse(spanIdString, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var spanId))
            {
                return spanId == 0 ? null : new SpanContext(traceId, spanId, traceState);
            }

            Log.Warning("Could not parse {HeaderName} headers: {HeaderValues}", W3CHeaderNames.TraceParent, string.Join(",", traceParentValues));
            return null;
        }

        private static string ExtractTraceState(IEnumerable<string> traceStateCollection)
        {
            if (traceStateCollection == null)
            {
                return null;
            }

            var keySet = new HashSet<string>();
            var result = new StringBuilder();
            foreach (var traceState in traceStateCollection)
            {
                var begin = 0;
                while (begin < traceState.Length)
                {
                    var length = traceState.Substring(begin).IndexOf(value: ',');
                    string listMember;
                    if (length != -1)
                    {
                        listMember = traceState.Substring(begin, length).Trim();
                        begin += length + 1;
                    }
                    else
                    {
                        listMember = traceState.Substring(begin).Trim();
                        begin = traceState.Length;
                    }

                    // https://github.com/w3c/trace-context/blob/master/spec/20-http_request_header_format.md#tracestate-header-field-values
                    if (string.IsNullOrEmpty(listMember))
                    {
                        // Empty and whitespace - only list members are allowed.
                        // Vendors MUST accept empty tracestate headers but SHOULD avoid sending them.
                        continue;
                    }

                    if (keySet.Count >= 32)
                    {
                        // https://github.com/w3c/trace-context/blob/master/spec/20-http_request_header_format.md#list
                        // test_tracestate_member_count_limit
                        return null;
                    }

                    var keyLength = listMember.IndexOf(value: '=');
                    if (keyLength == listMember.Length || keyLength == -1)
                    {
                        // Missing key or value in tracestate
                        return null;
                    }

                    var key = listMember.Substring(startIndex: 0, keyLength);
                    if (!ValidateKey(key))
                    {
                        // test_tracestate_key_illegal_characters in https://github.com/w3c/trace-context/blob/master/test/test.py
                        // test_tracestate_key_length_limit
                        // test_tracestate_key_illegal_vendor_format
                        return null;
                    }

                    var value = listMember.Substring(keyLength + 1);
                    if (!ValidateValue(value))
                    {
                        // test_tracestate_value_illegal_characters
                        return null;
                    }

                    // ValidateKey() call above has ensured the key does not contain upper case letters.
                    if (!keySet.Add(key))
                    {
                        // test_tracestate_duplicated_keys
                        return null;
                    }

                    if (result.Length > 0)
                    {
                        result.Append(value: ',');
                    }

                    result.Append(listMember);
                }
            }

            return result.ToString();
        }

        private static bool ValidateKey(string key)
        {
            // This implementation follows Trace Context v1 which has W3C Recommendation.
            // https://www.w3.org/TR/trace-context-1/#key
            // It will be slightly differently from the next version of specification in GitHub repository.

            // There are two format for the key. The length rule applies to both.
            if (key.Length <= 0 || key.Length > TraceStateKeyMaxLength)
            {
                return false;
            }

            // The first format:
            // key = lcalpha 0*255( lcalpha / DIGIT / "_" / "-"/ "*" / "/" )
            // lcalpha = % x61 - 7A; a - z
            // (There is an inconsistency in the expression above and the description in note.
            // Here is following the description in note:
            // "Identifiers MUST begin with a lowercase letter or a digit.")
            if (!IsLowerAlphaDigit(key[index: 0]))
            {
                return false;
            }

            var tenantLength = -1;
            for (var i = 1; i < key.Length; ++i)
            {
                char ch = key[i];
                if (ch == '@')
                {
                    tenantLength = i;
                    break;
                }

                if (!(IsLowerAlphaDigit(ch)
                    || ch == '_'
                    || ch == '-'
                    || ch == '*'
                    || ch == '/'))
                {
                    return false;
                }
            }

            if (tenantLength == -1)
            {
                // There is no "@" sign. The key follow the first format.
                return true;
            }

            // The second format:
            // key = (lcalpha / DIGIT) 0 * 240(lcalpha / DIGIT / "_" / "-" / "*" / "/") "@" lcalpha 0 * 13(lcalpha / DIGIT / "_" / "-" / "*" / "/")
            if (tenantLength == 0 || tenantLength > TraceStateKeyTenantMaxLength)
            {
                return false;
            }

            var vendorLength = key.Length - tenantLength - 1;
            if (vendorLength == 0 || vendorLength > TraceStateKeyVendorMaxLength)
            {
                return false;
            }

            for (var i = tenantLength + 1; i < key.Length; ++i)
            {
                char ch = key[i];
                if (!(IsLowerAlphaDigit(ch)
                    || ch == '_'
                    || ch == '-'
                    || ch == '*'
                    || ch == '/'))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool ValidateValue(string value)
        {
            // https://github.com/w3c/trace-context/blob/master/spec/20-http_request_header_format.md#value
            // value      = 0*255(chr) nblk-chr
            // nblk - chr = % x21 - 2B / % x2D - 3C / % x3E - 7E
            // chr        = % x20 / nblk - chr
            if (value.Length <= 0 || value.Length > TraceStateValueMaxLength)
            {
                return false;
            }

            for (var i = 0; i < value.Length - 1; ++i)
            {
                char c = value[i];
                if (!(c >= 0x20 && c <= 0x7E && c != 0x2C && c != 0x3D))
                {
                    return false;
                }
            }

            char last = value[value.Length - 1];
            return last >= 0x21 && last <= 0x7E && last != 0x2C && last != 0x3D;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsLowerAlphaDigit(char c)
        {
            return (c >= '0' && c <= '9') || (c >= 'a' && c <= 'z');
        }
    }
}
