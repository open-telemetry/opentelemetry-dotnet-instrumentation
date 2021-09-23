using System;
using System.Globalization;

namespace Datadog.Trace
{
    /// <summary>
    /// Class representing 64 or 128 bit TraceID. 64 bit representation is compatible with DataDog conventions (parsed from and to decimal representation).
    /// 128 bit representation is compatible with OpenTelemetry conventions (parsed from and to hexadecimal representation).
    /// </summary>
    public readonly struct TraceId : IEquatable<TraceId>
    {
        private readonly bool _isDataDogCompatible;
        private readonly string _string;

        private TraceId(long higher, long lower)
        {
            _isDataDogCompatible = false;
            Higher = (ulong)higher;
            Lower = (ulong)lower;
            _string = $"{Higher:x16}{Lower:x16}";
        }

        private TraceId(ulong lower)
        {
            _isDataDogCompatible = true;
            Higher = 0;
            Lower = lower;
            _string = Lower.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Gets TraceId with zero id.
        /// </summary>
        public static TraceId Zero => new(lower: 0);

        /// <summary>
        /// Gets higher 64 bits of 128 bit traceID.
        /// </summary>
        public ulong Higher { get; }

        /// <summary>
        /// Gets lower 64 bits of 128 bit traceID.
        /// </summary>
        public ulong Lower { get; }

        /// <summary>
        /// Indicates if two specified instances of TraceId are not equal.
        /// </summary>
        /// <param name="traceId1">First <see cref="TraceId"/></param>
        /// <param name="traceId2">Second <see cref="TraceId"/></param>
        /// <returns>True if instances are equal, false otherwise.</returns>
        public static bool operator ==(TraceId traceId1, TraceId traceId2)
        {
            return traceId1.Equals(traceId2);
        }

        /// <summary>
        /// Indicates if two specified instances of TraceId are not equal.
        /// </summary>
        /// <param name="traceId1">First <see cref="TraceId"/></param>
        /// <param name="traceId2">Second <see cref="TraceId"/></param>
        /// <returns>True if instances are not equal, false otherwise.</returns>
        public static bool operator !=(TraceId traceId1, TraceId traceId2)
        {
            return !(traceId1 == traceId2);
        }

        /// <summary>
        /// Creates random 128 bit traceId.
        /// </summary>
        /// <returns>Instance of randomly generated <see cref="TraceId"/>.</returns>
        public static TraceId CreateRandom()
        {
            var randomNumberGenerator = RandomNumberGenerator.Current;
            var higher = randomNumberGenerator.Next() & 0x7FFFFFFFFFFFFFFF;
            var lower = randomNumberGenerator.Next() & 0x7FFFFFFFFFFFFFFF;

            return new TraceId(higher, lower);
        }

        /// <summary>
        /// Creates random DataDog compatible 64 bit traceId.
        /// </summary>
        /// <returns>Instance of randomly generated <see cref="TraceId"/>.</returns>
        public static TraceId CreateRandomDataDogCompatible()
        {
            var randomNumberGenerator = RandomNumberGenerator.Current;
            var lower = (ulong)randomNumberGenerator.Next() & 0x7FFFFFFFFFFFFFFF;

            return new TraceId(lower);
        }

        /// <summary>
        /// Creates traceId from given 16 or 32 sign string representing traceId in hexadecimal format.
        /// </summary>
        /// <param name="id">16 or 32 sign string ID to be parsed.</param>
        /// <returns>Instance of <see cref="TraceId"/> representing the same traceId as the passed string.</returns>
        public static TraceId CreateFromString(string id)
        {
            try
            {
                switch (id.Length)
                {
                    case 16:
                    {
                        var lower = Convert.ToInt64(id, fromBase: 16);
                        return new TraceId(higher: 0, lower);
                    }

                    case 32:
                    {
                        var higherAsString = id.Substring(startIndex: 0, length: 16);
                        var lowerAsString = id.Substring(startIndex: 16, length: 16);

                        var higher = Convert.ToInt64(higherAsString, fromBase: 16);
                        var lower = Convert.ToInt64(lowerAsString, fromBase: 16);

                        return new TraceId(higher, lower);
                    }

                    default:
                    {
                        return Zero;
                    }
                }
            }
            catch (Exception ex) when (ex is ArgumentOutOfRangeException || ex is InvalidOperationException || ex is OverflowException || ex is FormatException)
            {
                return Zero;
            }
        }

        /// <summary>
        /// Creates DataDog compatible traceId from given string representing 64bit traceId in decimal format.
        /// </summary>
        /// <param name="id">String ID to be parsed.</param>
        /// <returns>Instance of 64bit <see cref="TraceId"/> representing the same traceId as the passed string.</returns>
        public static TraceId CreateDataDogCompatibleFromDecimalString(string id)
        {
            try
            {
                var lower = ulong.Parse(id);
                return new TraceId(lower);
            }
            catch (Exception ex) when (ex is ArgumentOutOfRangeException || ex is InvalidOperationException || ex is OverflowException || ex is FormatException)
            {
                return Zero;
            }
        }

        /// <summary>
        /// Creates 64 bit DataDog compatible traceId from given int.
        /// </summary>
        /// <param name="id">Int32 ID to be parsed.</param>
        /// <returns>Instance of <see cref="TraceId"/> representing the same traceId as the passed int.</returns>
        public static TraceId CreateFromInt(int id)
        {
            return new((ulong)id);
        }

        /// <summary>
        /// Creates 64 bit DataDog compatible traceId from given ulong.
        /// </summary>
        /// <param name="id">Ulong ID to be parsed.</param>
        /// <returns>Instance of <see cref="TraceId"/> representing the same traceId as the passed ulong.</returns>
        public static TraceId CreateFromUlong(ulong id)
        {
            return new(id);
        }

        /// <summary>
        /// For Datadog compatible traceId this returns decimal representation of the 64 bit id.
        /// For OpenTelemetry compatible traceId this returns hexadecimal representation of the 128 bit id.
        /// </summary>
        /// <returns>Depending on the traceId compatibility - dec or hex representation of <see cref="TraceId"/> as a string.</returns>
        public override string ToString() => _string;

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (!(obj is TraceId))
            {
                return false;
            }

            var traceId = (TraceId)obj;

            return Equals(traceId);
        }

        /// <summary>
        /// Indicates if this and another specified instance of TraceId are equal.
        /// </summary>
        /// <param name="other">Trace id to check equality against.</param>
        /// <returns>True if TraceIds are equal, false otherwise.</returns>
        public bool Equals(TraceId other)
        {
            return Lower == other.Lower && Higher == other.Higher && _isDataDogCompatible == other._isDataDogCompatible;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return HashCode.Combine(Higher, Lower, _isDataDogCompatible);
        }
    }
}
