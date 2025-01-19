using System;
using System.Runtime.CompilerServices;
using ModTek.Util.Stopwatch;

namespace ModTek.Features.Logging;

// effectively used only for byte arrays sized <= 14 (112bit)
internal static class FastMemCpy
{
    internal static unsafe void BlockCopy(byte[] src, int srcOffset, byte[] dst, int dstOffset, int length)
    {
        if (length > Threshold) // 700-1300 bytes are typical
        {
            Buffer.BlockCopy(src, srcOffset, dst, dstOffset, length);
        }
        else
        {
            fixed (byte* dstPtr = dst)
            {
                fixed (byte* srcPtr = src)
                {
                    Memcpy512(dstPtr + dstOffset, srcPtr + srcOffset, length);
                }
            }
        }
    }

    // from Buffer.memcpy* and optimized to use wider types like 128 and 256 bit
    // most gains from 128, some for 256, and almost none for 512. 1024 is negative.
    // faster than Buffer.BlockCopy but only until call overhead to extern method is overcome
    private static unsafe void Memcpy512(byte* dest, byte* src, int size)
    {
        {
            const int BatchSize = My512Bit.Size;
            for (; size >= BatchSize; size -= BatchSize)
            {
                *(My512Bit*)dest = *(My512Bit*)src;
                dest += BatchSize;
                src += BatchSize;
            }
        }
        {
            const int BatchSize = My256Bit.Size;
            for (; size >= BatchSize; size -= BatchSize)
            {
                *(My256Bit*)dest = *(My256Bit*)src;
                dest += BatchSize;
                src += BatchSize;
            }
        }
        {
            const int BatchSize = My128Bit.Size;
            for (; size >= BatchSize; size -= BatchSize)
            {
                *(My128Bit*)dest = *(My128Bit*)src;
                dest += BatchSize;
                src += BatchSize;
            }
        }
        {
            const int BatchSize = sizeof(ulong);
            for (; size >= BatchSize; size -= BatchSize)
            {
                *(ulong*)dest = *(ulong*)src;
                dest += BatchSize;
                src += BatchSize;
            }
        }
        {
            const int BatchSize = sizeof(ushort);
            for (; size >= BatchSize; size -= BatchSize)
            {
                *(ushort*)dest = *(ushort*)src;
                dest += BatchSize;
                src += BatchSize;
            }
        }
        if (size > 0)
        {
            *dest = *src;
        }
    }
    private struct My128Bit
    {
        internal const int Size = 2 * sizeof(ulong);
        internal ulong _00;
        internal ulong _01;
    }
    private struct My256Bit
    {
        internal const int Size = 2 * My128Bit.Size;
        internal My128Bit _00;
        internal My128Bit _01;
    }
    private struct My512Bit
    {
        internal const int Size = 2 * My256Bit.Size;
        internal My256Bit _00;
        internal My256Bit _01;
    }

    internal static readonly int Threshold = FindThreshold();
    private static unsafe int FindThreshold()
    {
        const int MaxSize = 4 * 1024;
        const int StepSize = 32;
        const int MinSize = 16;
        const int Steps = (MaxSize - MinSize) / StepSize;
        var byteBufferTicks = new long[Steps];
        var memCpyTicks = new long[Steps];
        var srcA = new byte[MaxSize];
        var srcB = new byte[MaxSize];
        for (var i = 0; i < MaxSize; i++)
        {
            srcA[i] = (byte)i;
            srcB[i] = (byte)i;
        }
        var dstA = new byte[MaxSize];
        var dstB = new byte[MaxSize];
        const int TestRunsPerSize = 100;

        var benchStart = MTStopwatch.GetTimestamp();
        do
        {
            for (var step = 0; step < Steps; step++)
            {
                var size = step * StepSize + MinSize;
                {
                    var start = MTStopwatch.GetTimestamp();
                    for (var run = 0; run < TestRunsPerSize; run++)
                    {
                        Buffer.BlockCopy(srcA, 0, dstA, 0, size);
                    }
                    byteBufferTicks[step] = MTStopwatch.GetTimestamp() - start;
                }
                {
                    var start = MTStopwatch.GetTimestamp();
                    for (var run = 0; run < TestRunsPerSize; run++)
                    {
                        fixed (byte* dst = dstB)
                        {
                            fixed (byte* src = srcB)
                            {
                                Memcpy512(dst, src, size);
                            }
                        }
                    }
                    memCpyTicks[step] = MTStopwatch.GetTimestamp() - start;
                }
            }
        } while (MTStopwatch.TimeSpanFromTicks(MTStopwatch.GetTimestamp() - benchStart).TotalMilliseconds < 1);

        for (var step = 0; step < Steps; step++)
        {
            if (memCpyTicks[step] > byteBufferTicks[step] )
            {
                return Math.Max((step - 1) * StepSize + MinSize, MinSize);
            }
        }
        return MaxSize;
    }
}