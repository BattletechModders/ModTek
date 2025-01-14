using System.Numerics;
using System.Runtime.CompilerServices;

namespace ModTek.Features.Logging;

extern alias MMB;

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
        var tableValue = s_countDigitsTable[MMB::System.Numerics.BitOperations.Log2(value)];
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
}
