using System.Runtime.CompilerServices;

namespace ModTek.Features.Logging;

// copied from .NET 9
internal static class FormattingHelpers
{
    internal static unsafe void WriteDigits(byte* positionPtr, long value, int digits)
    {
        const byte AsciiZero = (byte)'0';

        byte* current;
        for (current = positionPtr + digits - 1; current >= positionPtr; current--)
        {
            var temp = value + AsciiZero;
            value /= 10;
            *current = (byte)(temp - (value * 10));
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int CountDigits(uint value)
    {
        var tableValue = s_countDigitsTable[Log2(value)];
        return (int)((value + tableValue) >> 32);
    }
    // Algorithm based on https://lemire.me/blog/2021/06/03/computing-the-number-of-digits-of-an-integer-even-faster.
    private static readonly long[] s_countDigitsTable =
    [
        4294967296,
        8589934582,
        8589934582,
        8589934582,
        12884901788,
        12884901788,
        12884901788,
        17179868184,
        17179868184,
        17179868184,
        21474826480,
        21474826480,
        21474826480,
        21474826480,
        25769703776,
        25769703776,
        25769703776,
        30063771072,
        30063771072,
        30063771072,
        34349738368,
        34349738368,
        34349738368,
        34349738368,
        38554705664,
        38554705664,
        38554705664,
        41949672960,
        41949672960,
        41949672960,
        42949672960,
        42949672960,
    ];

    // following from MonoMod.Backports
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int Log2(uint value)
    {
        // The 0->0 contract is fulfilled by setting the LSB to 1.
        // Log(1) is 0, and setting the LSB for values > 1 does not change the log2 result.
        value |= 1;

        // Fallback contract is 0->0
        return Log2SoftwareFallback(value);
    }
    private static unsafe int Log2SoftwareFallback(uint value)
    {
        // No AggressiveInlining due to large method size
        // Has conventional contract 0->0 (Log(0) is undefined)

        // Fill trailing zeros with ones, eg 00010010 becomes 00011111
        value |= value >> 01;
        value |= value >> 02;
        value |= value >> 04;
        value |= value >> 08;
        value |= value >> 16;

        var offset = (value * 0x07C4ACDDu) >> 27;
        fixed (byte* ptr = s_log2DeBruijn)
        {
            return ptr[offset];
        }
    }
    private static readonly byte[] s_log2DeBruijn =
    [
        00, 09, 01, 10, 13, 21, 02, 29,
        11, 14, 16, 18, 22, 25, 03, 30,
        08, 12, 20, 28, 15, 17, 24, 07,
        19, 27, 23, 06, 26, 05, 04, 31
    ];
}
