using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Threading;
using ModTek.Util.Stopwatch;

namespace ModTek.Features.Logging;

// optimizes writing to a byte array:
// - loop unrolling -> allow some parallelism in the absence of SIMD, AVX could improve this 10x though
// - unsafe -> avoid array bounds checks (cpu branch prediction already skips those, so no gain except code size)
// - ascii conversion first -> quick char to byte conversions if compatible
// - byte based APIs -> avoids unnecessary conversions and allocations if possible
internal unsafe class FastBuffer
{
    internal FastBuffer(int initialCapacity = 16 * 1024)
    {
        EnlargeCapacity(initialCapacity);
    }

    internal int _length;
    private byte[] _buffer;
    internal int GetBytes(out byte[] bytes)
    {
        bytes = _buffer;
        return _length;
    }

    internal void Reset()
    {
        _length = 0;
    }

    private GCHandle _handle;
    private byte* _bufferPtr;
    internal void Pin()
    {
        if (_handle.IsAllocated)
        {
            return;
        }
        _handle = GCHandle.Alloc(_buffer, GCHandleType.Pinned);
        _bufferPtr = (byte*)_handle.AddrOfPinnedObject();
    }

    private bool _isG2;
    internal void Unpin()
    {
        if (_isG2)
        {
            return;
        }

        _isG2 = GC.GetGeneration(_buffer) == 2;
        if (_handle.IsAllocated)
        {
            _handle.Free();
        }
    }

    internal void Append(byte value)
    {
        var position = GetPointerAndIncrementLength(1);
        *position = value;
    }

    internal void Append(byte[] value)
    {
        var length = value.Length;
        var position = GetPointerAndIncrementLength(length);
        if (length > MemcpyThreshold)
        {
            var offset = (int)(position - _bufferPtr);
            Buffer.BlockCopy(value, 0, _buffer, offset, length);
        }
        else
        {
            fixed (byte* bytes = value)
            {
                Memcpy256(position, bytes, value.Length);
            }
        }
    }
    internal static string MemCpyStats()
    {
        var sb = new StringBuilder(10000);
        sb.Append("size,memCpyTicks,byteBufferTicks\n");
        for (var step = 0; step < Steps; step++)
        {
            var size = step * StepSize + MinSize;
            sb.Append(size);
            sb.Append(',');
            sb.Append(memCpyTicks[step]);
            sb.Append(',');
            sb.Append(byteBufferTicks[step]);
            sb.Append('\n');
        }
        return sb.ToString();
    }
    internal static readonly int MemcpyThreshold = FindMemCpyThreshold();
    private static long[] byteBufferTicks;
    private static long[] memCpyTicks;
    const int MaxSize = 4 * 1024;
    const int StepSize = 32;
    const int MinSize = 16;
    const int Steps = (MaxSize - MinSize) / StepSize;
    // TODO once we know that its always above some value, we can just set it and remove the benchmark
    private static int FindMemCpyThreshold()
    {
        byteBufferTicks = new long[Steps];
        memCpyTicks = new long[Steps];
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
                                Memcpy256(dst, src, size);
                            }
                        }
                    }
                    memCpyTicks[step] = MTStopwatch.GetTimestamp() - start;
                }
            }
        } while (MTStopwatch.TimeSpanFromTicks(MTStopwatch.GetTimestamp() - benchStart).TotalMilliseconds < 10);

        for (var step = 0; step < Steps; step++)
        {
            if (memCpyTicks[step] > byteBufferTicks[step] )
            {
                return Math.Max((step - 1) * StepSize + MinSize, MinSize);
            }
        }
        return MaxSize;
    }

    internal void Append(int value)
    {
        var digits = FormattingHelpers.CountDigits((uint)value);
        var position = GetPointerAndIncrementLength(digits);
        FormattingHelpers.WriteDigits(position, (uint)value, digits);
    }


    internal static readonly MTStopwatch AppendNativeStopwatch = new();
    internal static readonly MTStopwatch AppendManagedStopwatch = new();
    internal static readonly MTStopwatch AppendGetBytesStopwatch = new();
    private static long counter;
    internal void Append(string value)
    {
        //if (value.Length is >= 8 and < 32)
        // if (value.Length is >= 100)
        // {
        //     AppendGetBytes(value);
        //     return;
        // }
        var ok = Interlocked.Increment(ref counter);
        if (ok == 1_000)
        {
            AppendNativeStopwatch.Reset();
            AppendManagedStopwatch.Reset();
            AppendGetBytesStopwatch.Reset();
        }
        var l = _length;
        {
            var start = MTStopwatch.GetTimestamp();
            AppendNative(value);
            AppendNativeStopwatch.EndMeasurement(start);
        }
        _length = l;
        {
            var start = MTStopwatch.GetTimestamp();
            AppendManaged(value);
            AppendManagedStopwatch.EndMeasurement(start);
        }
        _length = l;
        {
            var start = MTStopwatch.GetTimestamp();
            AppendGetBytes(value);
            AppendGetBytesStopwatch.EndMeasurement(start);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void AppendGetBytes(string value)
    {
        const int Utf8MaxBytesPerChar = 4;
        EnsureCapacity(_length + value.Length * Utf8MaxBytesPerChar);
        _length += Encoding.UTF8.GetBytes(value, 0, value.Length, _buffer, _length);
    }

    private static int CountNonAscii(string value)
    {
        var processingCount = value.Length;
        var nonAsciiCount = 0;
        fixed (char* chars = value)
        {
            var ptr = (ulong*)chars;

            {
                const int IterSize = 8;
                for (; processingCount >= IterSize; processingCount -= IterSize)
                {
                    const ulong NonAsciiBitmask =
                        (1ul << (7 + 8 * 7)) +
                        (1ul << (7 + 8 * 5)) +
                        (1ul << (7 + 8 * 3)) +
                        (1ul << (7 + 8 * 1));
                    if ((*ptr & NonAsciiBitmask) != 0)
                    {
                        nonAsciiCount++;
                    }
                    ptr += IterSize;
                }
            }
            if (processingCount > 0)
            {
                const byte NonAsciiBitmask = 1 << 7;
                if ((*(byte*)ptr & NonAsciiBitmask) != 0)
                {
                    nonAsciiCount++;
                }
            }
            return nonAsciiCount;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void AppendNative(string value)
    {
        var valueLength = value.Length;
        if (valueLength == 0)
        {
            return;
        }

        fixed (char* chars = value)
        {
            var dstPtr = (IntPtr)(_bufferPtr + _length);
            var srcPtr = (IntPtr)chars;

            var processed = (int)convert_utf16le_to_utf8(srcPtr, (ulong)valueLength, dstPtr, (ulong)CapacityLeft);
            if (processed < 0)
            {
                EnsureCapacity(_length + valueLength - processed);
                processed = (int)convert_utf16le_to_utf8(srcPtr, (ulong)valueLength, dstPtr, (ulong)CapacityLeft);
            }
            _length += processed;
        }
    }

    [DllImport("libsimdutfexport", CallingConvention = CallingConvention.Cdecl)]
    [SuppressUnmanagedCodeSecurity]
    private static extern long convert_utf16le_to_utf8(IntPtr utf16, ulong utf16words, IntPtr utf8, ulong utf8space);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void AppendManaged(string value)
    {
        var processingCount = value.Length;
        if (processingCount == 0)
        {
            return;
        }

        // assume one byte per char, fallback will enlarge more defensively
        EnsureCapacity(_length + processingCount);

        fixed (char* chars = value)
        {
            var dstPtr = _bufferPtr + _length;
            var srcPtr = (byte*)chars + s_charLowBitsPosition;

            if (FastConvert(dstPtr, srcPtr, ref processingCount))
            {
                _length += value.Length;
            }
            else
            {
                // this is 2x slower than FastConvert (GetBytes has no fast ASCII path and no SIMD in this old .NET)
                var measurement = MTStopwatch.GetTimestamp();
                var charIndex = value.Length - processingCount;
                _length += charIndex;
                const int Utf8MaxBytesPerChar = 4;
                EnsureCapacity(_length + processingCount * Utf8MaxBytesPerChar);
                _length += Encoding.UTF8.GetBytes(value, charIndex, processingCount, _buffer, _length);
                UTF8FallbackStopwatch.EndMeasurement(measurement);
            }
        }
    }
    internal static readonly MTStopwatch UTF8FallbackStopwatch = new();
    private static readonly int s_charLowBitsPosition = GetLowerBytePosition();
    private static int GetLowerBytePosition()
    {
        var chars = stackalloc char[] { '1' };
        return *(byte*)chars == 0 ? 1 : 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool FastConvert(byte* dstPtr, byte* srcPtr, ref int processingCount)
    {
        return FastConvert128(dstPtr, srcPtr, ref processingCount);
    }

    // if utf16 is only ASCII7 we can just copy the lower bits to 1 byte
    // there is some parallelism achieved due to unrolling of the loop
    // batching also has an effect due to fewer ops overall
    // 8 is a sweat spot for unrolling and the ulong bit mask check
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool FastConvertMinimalistic(byte* dstPtr, byte* srcPtr, ref int processingCount)
    {
        for (var i = 0; i < processingCount; i++)
        {
            *(dstPtr + i) = *(srcPtr + i * 2);
        }
        for (var i = 0; i < processingCount; i++)
        {
            const byte NonAsciiBitmask = 1 << 7;
            if ((*(dstPtr + i) & NonAsciiBitmask) != 0)
            {
                return false;
            }
        }
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool FastConvertChunked(byte* dstPtr, byte* srcPtr, ref int processingCount)
    {
        const int ChunkSize = 256;
        var chunks = (processingCount + ChunkSize - 1) / ChunkSize;
        for (var chunk = 0; chunk < chunks; chunk++)
        {
            var start = chunk * ChunkSize;
            var end = Math.Min((chunk + 1) * ChunkSize, processingCount);
            for (var i = start; i < end; i++)
            {
                *(dstPtr + i) = *(srcPtr + i * 2);
            }
            for (var i = start; i < end; i++)
            {
                const byte NonAsciiBitmask = 1 << 7;
                if ((*(dstPtr + i) & NonAsciiBitmask) != 0)
                {
                    return false;
                }
            }
        }
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool FastConvert128(byte* dstPtr, byte* srcPtr, ref int processingCount)
    {
        {
            const int IterSize = 128;
            for (; processingCount >= IterSize; processingCount -= IterSize)
            {
                *(dstPtr +  0) = *(srcPtr +  0 * 2);
                *(dstPtr +  1) = *(srcPtr +  1 * 2);
                *(dstPtr +  2) = *(srcPtr +  2 * 2);
                *(dstPtr +  3) = *(srcPtr +  3 * 2);
                *(dstPtr +  4) = *(srcPtr +  4 * 2);
                *(dstPtr +  5) = *(srcPtr +  5 * 2);
                *(dstPtr +  6) = *(srcPtr +  6 * 2);
                *(dstPtr +  7) = *(srcPtr +  7 * 2);
                *(dstPtr +  8) = *(srcPtr +  8 * 2);
                *(dstPtr +  9) = *(srcPtr +  9 * 2);
                *(dstPtr + 10) = *(srcPtr + 10 * 2);
                *(dstPtr + 11) = *(srcPtr + 11 * 2);
                *(dstPtr + 12) = *(srcPtr + 12 * 2);
                *(dstPtr + 13) = *(srcPtr + 13 * 2);
                *(dstPtr + 14) = *(srcPtr + 14 * 2);
                *(dstPtr + 15) = *(srcPtr + 15 * 2);
                *(dstPtr + 16) = *(srcPtr + 16 * 2);
                *(dstPtr + 17) = *(srcPtr + 17 * 2);
                *(dstPtr + 18) = *(srcPtr + 18 * 2);
                *(dstPtr + 19) = *(srcPtr + 19 * 2);
                *(dstPtr + 20) = *(srcPtr + 20 * 2);
                *(dstPtr + 21) = *(srcPtr + 21 * 2);
                *(dstPtr + 22) = *(srcPtr + 22 * 2);
                *(dstPtr + 23) = *(srcPtr + 23 * 2);
                *(dstPtr + 24) = *(srcPtr + 24 * 2);
                *(dstPtr + 25) = *(srcPtr + 25 * 2);
                *(dstPtr + 26) = *(srcPtr + 26 * 2);
                *(dstPtr + 27) = *(srcPtr + 27 * 2);
                *(dstPtr + 28) = *(srcPtr + 28 * 2);
                *(dstPtr + 29) = *(srcPtr + 29 * 2);
                *(dstPtr + 30) = *(srcPtr + 30 * 2);
                *(dstPtr + 31) = *(srcPtr + 31 * 2);
                *(dstPtr + 32) = *(srcPtr + 32 * 2);
                *(dstPtr + 33) = *(srcPtr + 33 * 2);
                *(dstPtr + 34) = *(srcPtr + 34 * 2);
                *(dstPtr + 35) = *(srcPtr + 35 * 2);
                *(dstPtr + 36) = *(srcPtr + 36 * 2);
                *(dstPtr + 37) = *(srcPtr + 37 * 2);
                *(dstPtr + 38) = *(srcPtr + 38 * 2);
                *(dstPtr + 39) = *(srcPtr + 39 * 2);
                *(dstPtr + 40) = *(srcPtr + 40 * 2);
                *(dstPtr + 41) = *(srcPtr + 41 * 2);
                *(dstPtr + 42) = *(srcPtr + 42 * 2);
                *(dstPtr + 43) = *(srcPtr + 43 * 2);
                *(dstPtr + 44) = *(srcPtr + 44 * 2);
                *(dstPtr + 45) = *(srcPtr + 45 * 2);
                *(dstPtr + 46) = *(srcPtr + 46 * 2);
                *(dstPtr + 47) = *(srcPtr + 47 * 2);
                *(dstPtr + 48) = *(srcPtr + 48 * 2);
                *(dstPtr + 49) = *(srcPtr + 49 * 2);
                *(dstPtr + 50) = *(srcPtr + 50 * 2);
                *(dstPtr + 51) = *(srcPtr + 51 * 2);
                *(dstPtr + 52) = *(srcPtr + 52 * 2);
                *(dstPtr + 53) = *(srcPtr + 53 * 2);
                *(dstPtr + 54) = *(srcPtr + 54 * 2);
                *(dstPtr + 55) = *(srcPtr + 55 * 2);
                *(dstPtr + 56) = *(srcPtr + 56 * 2);
                *(dstPtr + 57) = *(srcPtr + 57 * 2);
                *(dstPtr + 58) = *(srcPtr + 58 * 2);
                *(dstPtr + 59) = *(srcPtr + 59 * 2);
                *(dstPtr + 60) = *(srcPtr + 60 * 2);
                *(dstPtr + 61) = *(srcPtr + 61 * 2);
                *(dstPtr + 62) = *(srcPtr + 62 * 2);
                *(dstPtr + 63) = *(srcPtr + 63 * 2);
                *(dstPtr + 64) = *(srcPtr + 64 * 2);
                *(dstPtr + 65) = *(srcPtr + 65 * 2);
                *(dstPtr + 66) = *(srcPtr + 66 * 2);
                *(dstPtr + 67) = *(srcPtr + 67 * 2);
                *(dstPtr + 68) = *(srcPtr + 68 * 2);
                *(dstPtr + 69) = *(srcPtr + 69 * 2);
                *(dstPtr + 70) = *(srcPtr + 70 * 2);
                *(dstPtr + 71) = *(srcPtr + 71 * 2);
                *(dstPtr + 72) = *(srcPtr + 72 * 2);
                *(dstPtr + 73) = *(srcPtr + 73 * 2);
                *(dstPtr + 74) = *(srcPtr + 74 * 2);
                *(dstPtr + 75) = *(srcPtr + 75 * 2);
                *(dstPtr + 76) = *(srcPtr + 76 * 2);
                *(dstPtr + 77) = *(srcPtr + 77 * 2);
                *(dstPtr + 78) = *(srcPtr + 78 * 2);
                *(dstPtr + 79) = *(srcPtr + 79 * 2);
                *(dstPtr + 80) = *(srcPtr + 80 * 2);
                *(dstPtr + 81) = *(srcPtr + 81 * 2);
                *(dstPtr + 82) = *(srcPtr + 82 * 2);
                *(dstPtr + 83) = *(srcPtr + 83 * 2);
                *(dstPtr + 84) = *(srcPtr + 84 * 2);
                *(dstPtr + 85) = *(srcPtr + 85 * 2);
                *(dstPtr + 86) = *(srcPtr + 86 * 2);
                *(dstPtr + 87) = *(srcPtr + 87 * 2);
                *(dstPtr + 88) = *(srcPtr + 88 * 2);
                *(dstPtr + 89) = *(srcPtr + 89 * 2);
                *(dstPtr + 90) = *(srcPtr + 90 * 2);
                *(dstPtr + 91) = *(srcPtr + 91 * 2);
                *(dstPtr + 92) = *(srcPtr + 92 * 2);
                *(dstPtr + 93) = *(srcPtr + 93 * 2);
                *(dstPtr + 94) = *(srcPtr + 94 * 2);
                *(dstPtr + 95) = *(srcPtr + 95 * 2);
                *(dstPtr + 96) = *(srcPtr + 96 * 2);
                *(dstPtr + 97) = *(srcPtr + 97 * 2);
                *(dstPtr + 98) = *(srcPtr + 98 * 2);
                *(dstPtr + 99) = *(srcPtr + 99 * 2);
                *(dstPtr +100) = *(srcPtr +100 * 2);
                *(dstPtr +101) = *(srcPtr +101 * 2);
                *(dstPtr +102) = *(srcPtr +102 * 2);
                *(dstPtr +103) = *(srcPtr +103 * 2);
                *(dstPtr +104) = *(srcPtr +104 * 2);
                *(dstPtr +105) = *(srcPtr +105 * 2);
                *(dstPtr +106) = *(srcPtr +106 * 2);
                *(dstPtr +107) = *(srcPtr +107 * 2);
                *(dstPtr +108) = *(srcPtr +108 * 2);
                *(dstPtr +109) = *(srcPtr +109 * 2);
                *(dstPtr +110) = *(srcPtr +110 * 2);
                *(dstPtr +111) = *(srcPtr +111 * 2);
                *(dstPtr +112) = *(srcPtr +112 * 2);
                *(dstPtr +113) = *(srcPtr +113 * 2);
                *(dstPtr +114) = *(srcPtr +114 * 2);
                *(dstPtr +115) = *(srcPtr +115 * 2);
                *(dstPtr +116) = *(srcPtr +116 * 2);
                *(dstPtr +117) = *(srcPtr +117 * 2);
                *(dstPtr +118) = *(srcPtr +118 * 2);
                *(dstPtr +119) = *(srcPtr +119 * 2);
                *(dstPtr +120) = *(srcPtr +120 * 2);
                *(dstPtr +121) = *(srcPtr +121 * 2);
                *(dstPtr +122) = *(srcPtr +122 * 2);
                *(dstPtr +123) = *(srcPtr +123 * 2);
                *(dstPtr +124) = *(srcPtr +124 * 2);
                *(dstPtr +125) = *(srcPtr +125 * 2);
                *(dstPtr +126) = *(srcPtr +126 * 2);
                *(dstPtr +127) = *(srcPtr +127 * 2);

                const ulong NonAsciiBitmask =
                    (1ul << (7 + 8 * 7)) +
                    (1ul << (7 + 8 * 6)) +
                    (1ul << (7 + 8 * 5)) +
                    (1ul << (7 + 8 * 4)) +
                    (1ul << (7 + 8 * 3)) +
                    (1ul << (7 + 8 * 2)) +
                    (1ul << (7 + 8 * 1)) +
                    (1ul << (7 + 8 * 0));
                if ((*((ulong*)dstPtr+0) & NonAsciiBitmask) != 0)
                {
                    return false;
                }
                if ((*((ulong*)dstPtr+1) & NonAsciiBitmask) != 0)
                {
                    return false;
                }
                if ((*((ulong*)dstPtr+2) & NonAsciiBitmask) != 0)
                {
                    return false;
                }
                if ((*((ulong*)dstPtr+3) & NonAsciiBitmask) != 0)
                {
                    return false;
                }
                if ((*((ulong*)dstPtr+4) & NonAsciiBitmask) != 0)
                {
                    return false;
                }
                if ((*((ulong*)dstPtr+5) & NonAsciiBitmask) != 0)
                {
                    return false;
                }
                if ((*((ulong*)dstPtr+6) & NonAsciiBitmask) != 0)
                {
                    return false;
                }
                if ((*((ulong*)dstPtr+7) & NonAsciiBitmask) != 0)
                {
                    return false;
                }
                if ((*((ulong*)dstPtr+8) & NonAsciiBitmask) != 0)
                {
                    return false;
                }
                if ((*((ulong*)dstPtr+9) & NonAsciiBitmask) != 0)
                {
                    return false;
                }
                if ((*((ulong*)dstPtr+10) & NonAsciiBitmask) != 0)
                {
                    return false;
                }
                if ((*((ulong*)dstPtr+11) & NonAsciiBitmask) != 0)
                {
                    return false;
                }
                if ((*((ulong*)dstPtr+12) & NonAsciiBitmask) != 0)
                {
                    return false;
                }
                if ((*((ulong*)dstPtr+13) & NonAsciiBitmask) != 0)
                {
                    return false;
                }
                if ((*((ulong*)dstPtr+14) & NonAsciiBitmask) != 0)
                {
                    return false;
                }
                if ((*((ulong*)dstPtr+15) & NonAsciiBitmask) != 0)
                {
                    return false;
                }
                dstPtr += IterSize;
                srcPtr += 2*IterSize;
            }
        }

        {
            const int IterSize = 32;
            for (; processingCount >= IterSize; processingCount -= IterSize)
            {
                *(dstPtr + 0) = *(srcPtr + 0 * 2);
                *(dstPtr + 1) = *(srcPtr + 1 * 2);
                *(dstPtr + 2) = *(srcPtr + 2 * 2);
                *(dstPtr + 3) = *(srcPtr + 3 * 2);
                *(dstPtr + 4) = *(srcPtr + 4 * 2);
                *(dstPtr + 5) = *(srcPtr + 5 * 2);
                *(dstPtr + 6) = *(srcPtr + 6 * 2);
                *(dstPtr + 7) = *(srcPtr + 7 * 2);
                *(dstPtr + 8) = *(srcPtr + 8 * 2);
                *(dstPtr + 9) = *(srcPtr + 9 * 2);
                *(dstPtr +10) = *(srcPtr +10 * 2);
                *(dstPtr +11) = *(srcPtr +11 * 2);
                *(dstPtr +12) = *(srcPtr +12 * 2);
                *(dstPtr +13) = *(srcPtr +13 * 2);
                *(dstPtr +14) = *(srcPtr +14 * 2);
                *(dstPtr +15) = *(srcPtr +15 * 2);
                *(dstPtr +16) = *(srcPtr +16 * 2);
                *(dstPtr +17) = *(srcPtr +17 * 2);
                *(dstPtr +18) = *(srcPtr +18 * 2);
                *(dstPtr +19) = *(srcPtr +19 * 2);
                *(dstPtr +20) = *(srcPtr +20 * 2);
                *(dstPtr +21) = *(srcPtr +21 * 2);
                *(dstPtr +22) = *(srcPtr +22 * 2);
                *(dstPtr +23) = *(srcPtr +23 * 2);
                *(dstPtr +24) = *(srcPtr +24 * 2);
                *(dstPtr +25) = *(srcPtr +25 * 2);
                *(dstPtr +26) = *(srcPtr +26 * 2);
                *(dstPtr +27) = *(srcPtr +27 * 2);
                *(dstPtr +28) = *(srcPtr +28 * 2);
                *(dstPtr +29) = *(srcPtr +29 * 2);
                *(dstPtr +30) = *(srcPtr +30 * 2);
                *(dstPtr +31) = *(srcPtr +31 * 2);

                const ulong NonAsciiBitmask =
                    (1ul << (7 + 8 * 7)) +
                    (1ul << (7 + 8 * 6)) +
                    (1ul << (7 + 8 * 5)) +
                    (1ul << (7 + 8 * 4)) +
                    (1ul << (7 + 8 * 3)) +
                    (1ul << (7 + 8 * 2)) +
                    (1ul << (7 + 8 * 1)) +
                    (1ul << (7 + 8 * 0));
                if ((*((ulong*)dstPtr+0) & NonAsciiBitmask) != 0)
                {
                    return false;
                }
                if ((*((ulong*)dstPtr+1) & NonAsciiBitmask) != 0)
                {
                    return false;
                }
                if ((*((ulong*)dstPtr+2) & NonAsciiBitmask) != 0)
                {
                    return false;
                }
                if ((*((ulong*)dstPtr+3) & NonAsciiBitmask) != 0)
                {
                    return false;
                }
                dstPtr += IterSize;
                srcPtr += 2*IterSize;
            }
        }

        {
            const int IterSize = 8;
            for (; processingCount >= IterSize; processingCount -= IterSize)
            {
                *(dstPtr + 0) = *(srcPtr + 0 * 2);
                *(dstPtr + 1) = *(srcPtr + 1 * 2);
                *(dstPtr + 2) = *(srcPtr + 2 * 2);
                *(dstPtr + 3) = *(srcPtr + 3 * 2);
                *(dstPtr + 4) = *(srcPtr + 4 * 2);
                *(dstPtr + 5) = *(srcPtr + 5 * 2);
                *(dstPtr + 6) = *(srcPtr + 6 * 2);
                *(dstPtr + 7) = *(srcPtr + 7 * 2);

                const ulong NonAsciiBitmask =
                        (1ul << (7 + 8 * 7)) +
                        (1ul << (7 + 8 * 6)) +
                        (1ul << (7 + 8 * 5)) +
                        (1ul << (7 + 8 * 4)) +
                        (1ul << (7 + 8 * 3)) +
                        (1ul << (7 + 8 * 2)) +
                        (1ul << (7 + 8 * 1)) +
                        (1ul << (7 + 8 * 0));
                if ((*(ulong*)dstPtr & NonAsciiBitmask) != 0)
                {
                    return false;
                }
                dstPtr += IterSize;
                srcPtr += 2*IterSize;
            }
        }

        {
            const int IterSize = 2;
            for (; processingCount >= IterSize; processingCount -= IterSize)
            {
                *(dstPtr + 0) = *(srcPtr + 0 * 2);
                *(dstPtr + 1) = *(srcPtr + 1 * 2);

                const ushort NonAsciiBitmask =
                    (1 << (7 + 8 * 1)) +
                    (1 << (7 + 8 * 0));
                if ((*(ushort*)dstPtr & NonAsciiBitmask) != 0)
                {
                    return false;
                }
                dstPtr += IterSize;
                srcPtr += 2*IterSize;
            }
        }

        if (processingCount > 0)
        {
            *(dstPtr + 0) = *(srcPtr + 0 * 2);

            const byte NonAsciiBitmask = 1 << 7;
            if ((*dstPtr & NonAsciiBitmask) != 0)
            {
                return false;
            }
        }

        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool FastConvert32(byte* dstPtr, byte* srcPtr, ref int processingCount)
    {
        {
            const int IterSize = 32;
            for (; processingCount >= IterSize; processingCount -= IterSize)
            {
                *(dstPtr + 0) = *(srcPtr + 0 * 2);
                *(dstPtr + 1) = *(srcPtr + 1 * 2);
                *(dstPtr + 2) = *(srcPtr + 2 * 2);
                *(dstPtr + 3) = *(srcPtr + 3 * 2);
                *(dstPtr + 4) = *(srcPtr + 4 * 2);
                *(dstPtr + 5) = *(srcPtr + 5 * 2);
                *(dstPtr + 6) = *(srcPtr + 6 * 2);
                *(dstPtr + 7) = *(srcPtr + 7 * 2);
                *(dstPtr + 8) = *(srcPtr + 8 * 2);
                *(dstPtr + 9) = *(srcPtr + 9 * 2);
                *(dstPtr +10) = *(srcPtr +10 * 2);
                *(dstPtr +11) = *(srcPtr +11 * 2);
                *(dstPtr +12) = *(srcPtr +12 * 2);
                *(dstPtr +13) = *(srcPtr +13 * 2);
                *(dstPtr +14) = *(srcPtr +14 * 2);
                *(dstPtr +15) = *(srcPtr +15 * 2);
                *(dstPtr +16) = *(srcPtr +16 * 2);
                *(dstPtr +17) = *(srcPtr +17 * 2);
                *(dstPtr +18) = *(srcPtr +18 * 2);
                *(dstPtr +19) = *(srcPtr +19 * 2);
                *(dstPtr +20) = *(srcPtr +20 * 2);
                *(dstPtr +21) = *(srcPtr +21 * 2);
                *(dstPtr +22) = *(srcPtr +22 * 2);
                *(dstPtr +23) = *(srcPtr +23 * 2);
                *(dstPtr +24) = *(srcPtr +24 * 2);
                *(dstPtr +25) = *(srcPtr +25 * 2);
                *(dstPtr +26) = *(srcPtr +26 * 2);
                *(dstPtr +27) = *(srcPtr +27 * 2);
                *(dstPtr +28) = *(srcPtr +28 * 2);
                *(dstPtr +29) = *(srcPtr +29 * 2);
                *(dstPtr +30) = *(srcPtr +30 * 2);
                *(dstPtr +31) = *(srcPtr +31 * 2);

                const ulong NonAsciiBitmask =
                    (1ul << (7 + 8 * 7)) +
                    (1ul << (7 + 8 * 6)) +
                    (1ul << (7 + 8 * 5)) +
                    (1ul << (7 + 8 * 4)) +
                    (1ul << (7 + 8 * 3)) +
                    (1ul << (7 + 8 * 2)) +
                    (1ul << (7 + 8 * 1)) +
                    (1ul << (7 + 8 * 0));
                if ((*((ulong*)dstPtr+0) & NonAsciiBitmask) != 0)
                {
                    return false;
                }
                if ((*((ulong*)dstPtr+1) & NonAsciiBitmask) != 0)
                {
                    return false;
                }
                if ((*((ulong*)dstPtr+2) & NonAsciiBitmask) != 0)
                {
                    return false;
                }
                if ((*((ulong*)dstPtr+3) & NonAsciiBitmask) != 0)
                {
                    return false;
                }
                dstPtr += IterSize;
                srcPtr += 2*IterSize;
            }
        }

        {
            const int IterSize = 8;
            for (; processingCount >= IterSize; processingCount -= IterSize)
            {
                *(dstPtr + 0) = *(srcPtr + 0 * 2);
                *(dstPtr + 1) = *(srcPtr + 1 * 2);
                *(dstPtr + 2) = *(srcPtr + 2 * 2);
                *(dstPtr + 3) = *(srcPtr + 3 * 2);
                *(dstPtr + 4) = *(srcPtr + 4 * 2);
                *(dstPtr + 5) = *(srcPtr + 5 * 2);
                *(dstPtr + 6) = *(srcPtr + 6 * 2);
                *(dstPtr + 7) = *(srcPtr + 7 * 2);

                const ulong NonAsciiBitmask =
                        (1ul << (7 + 8 * 7)) +
                        (1ul << (7 + 8 * 6)) +
                        (1ul << (7 + 8 * 5)) +
                        (1ul << (7 + 8 * 4)) +
                        (1ul << (7 + 8 * 3)) +
                        (1ul << (7 + 8 * 2)) +
                        (1ul << (7 + 8 * 1)) +
                        (1ul << (7 + 8 * 0));
                if ((*(ulong*)dstPtr & NonAsciiBitmask) != 0)
                {
                    return false;
                }
                dstPtr += IterSize;
                srcPtr += 2*IterSize;
            }
        }

        {
            const int IterSize = 2;
            for (; processingCount >= IterSize; processingCount -= IterSize)
            {
                *(dstPtr + 0) = *(srcPtr + 0 * 2);
                *(dstPtr + 1) = *(srcPtr + 1 * 2);

                const ushort NonAsciiBitmask =
                    (1 << (7 + 8 * 1)) +
                    (1 << (7 + 8 * 0));
                if ((*(ushort*)dstPtr & NonAsciiBitmask) != 0)
                {
                    return false;
                }
                dstPtr += IterSize;
                srcPtr += 2*IterSize;
            }
        }

        if (processingCount > 0)
        {
            *(dstPtr + 0) = *(srcPtr + 0 * 2);

            const byte NonAsciiBitmask = 1 << 7;
            if ((*dstPtr & NonAsciiBitmask) != 0)
            {
                return false;
            }
        }

        return true;
    }

    internal void Append(DateTime value)
    {
        AppendTime(value.Hour, value.Minute, value.Second, value.Ticks);
    }

    internal void Append(TimeSpan value)
    {
        AppendTime(value.Hours, value.Minutes, value.Seconds, value.Ticks);
    }

    private void AppendTime(int hours, int minutes, int seconds, long ticks)
    {
        var position = GetPointerAndIncrementLength(17);
        FormattingHelpers.WriteDigits(position, hours, 2);
        position[2] = (byte)':';
        FormattingHelpers.WriteDigits(position + 3, minutes, 2);
        position[5] = (byte)':';
        FormattingHelpers.WriteDigits(position + 6, seconds, 2);
        position[8] = (byte)'.';
        FormattingHelpers.WriteDigits(position + 9, ticks, 7);
        position[16] = (byte)' ';
    }

    private byte* GetPointerAndIncrementLength(int increment)
    {
        var length = _length;
        var requiredCapacity = _length + increment;
        EnsureCapacity(requiredCapacity);
        _length = requiredCapacity;
        return _bufferPtr + length;
    }

    private int CapacityLeft => _buffer.Length - _length;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void EnsureCapacity(int targetLength)
    {
        if (_buffer.Length < targetLength)
        {
            EnlargeCapacity(targetLength);
        }
    }

    private void EnlargeCapacity(int targetLength)
    {
        var newBuffer = new byte[targetLength];
        if (_buffer != null)
        {
            // block copy is faster for larger byte arrays
            Buffer.BlockCopy(_buffer, 0, newBuffer, 0, _length);
            try
            {
                _handle.Free();
            }
            catch
            {
                _buffer = null;
                _bufferPtr = null;
                _isG2 = false;
            }
        }
        _buffer = newBuffer;
        _handle = GCHandle.Alloc(newBuffer, GCHandleType.Pinned);
        _bufferPtr = (byte*)_handle.AddrOfPinnedObject();;
        _isG2 = false;
    }

    // from Buffer.memcpy* and optimized to use wider types like 128 and 256 bit
    // most gains from 128, some for 256, and almost none for 512 (therefore left out)
    internal static void Memcpy256(byte* dest, byte* src, int size)
    {
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

    ~FastBuffer()
    {
        try
        {
            _handle.Free();
        }
        catch
        {
            // ignored
        }
    }
}