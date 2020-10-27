using System;

namespace Datadog.AutoInstrumentation.ActivityExporter
{
    /// <summary>
    /// Currently not actually used in prod. Contains APIs for an Actvity Export POC prototype.
    /// </summary>
    internal static class Convert
    {
        public static ulong HexStringToUInt63(string hexStr, int byteCount)
        {
            ulong val = Convert.HexStringToUInt64(hexStr, byteCount);
            return val & 0x7FFFFFFFFFFFFFFF;
        }

        public static ulong HexStringToUInt64(string hexStr, int byteCount = -1)
        {
            if (byteCount < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(byteCount));
            }

            if (hexStr.Length != byteCount * 2)
            {
                throw new ArgumentException($"The specified {nameof(hexStr)} is expected to have exacty {byteCount * 2}"
                                          + $" hex chars ({byteCount} bytes), but it actually has {hexStr.Length} chars.");
            }

            ulong spanValue = 0;

            unchecked
            {
                char c1, c2;
                for (int b = 0; b < byteCount; b++)
                {
                    c1 = hexStr[(b << 1)];
                    c2 = hexStr[(b << 1) + 1];

                    spanValue <<= 4;
                    spanValue |= HexCharToUInt64(c1);
                    spanValue <<= 4;
                    spanValue |= HexCharToUInt64(c2);
                }
            }

            return spanValue;
        }

        private static ulong HexCharToUInt64(char hexChar)
        {
            switch (hexChar)
            {
                case '0': return 0;
                case '1': return 1;
                case '2': return 2;
                case '3': return 3;
                case '4': return 4;
                case '5': return 5;
                case '6': return 6;
                case '7': return 7;
                case '8': return 8;
                case '9': return 9;
                case 'a':
                case 'A': return 10;
                case 'b':
                case 'B': return 11;
                case 'c':
                case 'C': return 12;
                case 'd':
                case 'D': return 13;
                case 'e':
                case 'E': return 14;
                case 'f':
                case 'F': return 15;
                default:
                    throw new ArgumentOutOfRangeException(paramName: nameof(hexChar), message: $"Specified character ('{hexChar}') is not a valid hex character.");
            }
        }
    }
}
