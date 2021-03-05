using System;

namespace Datadog.Trace
{
    /// <summary>
    /// RandomNumberGenerator implementation is the 64-bit random number generator based on the Xoshiro256StarStar algorithm (known as shift-register generators).
    /// </summary>
    internal class RandomNumberGenerator
    {
        [ThreadStatic]
        private static RandomNumberGenerator _random;

        private ulong _s0;
        private ulong _s1;
        private ulong _s2;
        private ulong _s3;

#if ALLOW_PARTIALLY_TRUSTED_CALLERS
        [System.Security.SecuritySafeCriticalAttribute]
#endif
        private unsafe RandomNumberGenerator()
        {
            do
            {
                var g1 = Guid.NewGuid();
                var g2 = Guid.NewGuid();
                var g1p = (ulong*)&g1;
                var g2p = (ulong*)&g2;
                _s0 = *g1p;
                _s1 = *(g1p + 1);
                _s2 = *g2p;
                _s3 = *(g2p + 1);

                // Guid uses the 4 most significant bits of the first long as the version which would be fixed and not randomized.
                // and uses 2 other bits in the second long for variants which would be fixed and not randomized too.
                // let's overwrite the fixed bits in each long part by the other long.
                _s0 = (_s0 & 0x0FFFFFFFFFFFFFFF) | (_s1 & 0xF000000000000000);
                _s2 = (_s2 & 0x0FFFFFFFFFFFFFFF) | (_s3 & 0xF000000000000000);
                _s1 = (_s1 & 0xFFFFFFFFFFFFFF3F) | (_s0 & 0x00000000000000C0);
                _s3 = (_s3 & 0xFFFFFFFFFFFFFF3F) | (_s2 & 0x00000000000000C0);
            }
            while ((_s0 | _s1 | _s2 | _s3) == 0);
        }

        public static RandomNumberGenerator Current
        {
            get => _random ??= new RandomNumberGenerator();
        }

        public long Next()
        {
            ulong result = Rol64(_s1 * 5, 7) * 9;
            ulong t = _s1 << 17;

            _s2 ^= _s0;
            _s3 ^= _s1;
            _s1 ^= _s2;
            _s0 ^= _s3;

            _s2 ^= t;
            _s3 = Rol64(_s3, 45);

            return (long)result;
        }

        private static ulong Rol64(ulong x, int k) => (x << k) | (x >> (64 - k));
    }
}
