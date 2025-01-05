using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace ModTek.Util;

// .NET 9 XoshiroImpl
internal sealed class FastRandom
{
    private ulong _s0, _s1, _s2, _s3;

    internal FastRandom()
    {
        var rnd = new Random();
        do
        {
            _s0 = ((ulong)rnd.Next() << 32) | (uint)rnd.Next();
            _s1 = ((ulong)rnd.Next() << 32) | (uint)rnd.Next();
            _s2 = ((ulong)rnd.Next() << 32) | (uint)rnd.Next();
            _s3 = ((ulong)rnd.Next() << 32) | (uint)rnd.Next();
        } while ((_s0 | _s1 | _s2 | _s3) == 0); // at least one value must be non-zero
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal ulong NextUInt64()
    {
        ulong s0 = _s0, s1 = _s1, s2 = _s2, s3 = _s3;

        var result = BitOperations.RotateLeft(s1 * 5, 7) * 9;
        var t = s1 << 17;

        s2 ^= s0;
        s3 ^= s1;
        s1 ^= s2;
        s0 ^= s3;

        s2 ^= t;
        s3 = BitOperations.RotateLeft(s3, 45);

        _s0 = s0;
        _s1 = s1;
        _s2 = s2;
        _s3 = s3;

        return result;
    }
}