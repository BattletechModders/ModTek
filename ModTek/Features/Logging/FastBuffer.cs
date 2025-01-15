using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
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
                Memcpy512(position, bytes, value.Length);
            }
        }
    }
    internal static readonly int MemcpyThreshold = FindMemCpyThreshold();
    // TODO once we know that its always above some value, we can just set it and remove the benchmark
    private static int FindMemCpyThreshold()
    {
        const int MaxSize = 4 * 1024;
        var srcA = new byte[MaxSize];
        var dstA = new byte[MaxSize];
        var dst = stackalloc byte[MaxSize];

        const int TestRunsPerSize = 100;
        var byteBufferTicks = new long[TestRunsPerSize];
        var memCpyTicks = new long[TestRunsPerSize];

        const int WarmupCount = 100;
        for (var w = 0; w < WarmupCount + 1; w++)
        {
            var shouldMeasure = w == WarmupCount;
            const int StepSize = 256;
            const int ThresholdMin = 256;
            for (var size=ThresholdMin+StepSize; size<=MaxSize; size+=StepSize) {
                for (var run = 0; run < TestRunsPerSize; run++)
                {
                    var start = shouldMeasure ? MTStopwatch.GetTimestamp() : 0;
                    Buffer.BlockCopy(srcA, 0, dstA, 0, size);
                    if (shouldMeasure)
                    {
                        byteBufferTicks[run] = MTStopwatch.GetTimestamp() - start;
                    }
                }
                for (var run = 0; run < TestRunsPerSize; run++)
                {
                    var start = shouldMeasure ? MTStopwatch.GetTimestamp() : 0;
                    fixed (byte* bytes = srcA)
                    {
                        Memcpy512(dst, bytes, size);
                    }
                    if (shouldMeasure)
                    {
                        memCpyTicks[run] = MTStopwatch.GetTimestamp() - start;
                    }
                }
                if (shouldMeasure)
                {
                    if (MTStopwatch.TicksMin(memCpyTicks) > MTStopwatch.TicksMin(byteBufferTicks))
                    {
                        return size - StepSize;
                    }
                }
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

    internal void Append(string value)
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
                // this is 10x slower or more (GetBytes has no fast ASCII path and no SIMD in this old .NET)
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
    // if utf16 is only ASCII7 we can just copy the lower bits to 1 byte
    // there is some parallelism achieved due to unrolling of the loop
    // batching also has an effect due to fewer ops overall
    // 8 is a sweat spot for unrolling and the ulong bit mask check
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool FastConvert(byte* dstPtr, byte* srcPtr,  ref int processingCount)
    {
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

    internal static void Memcpy64(byte* dest, byte* src, int size)
    {
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

    internal static void Memcpy128(byte* dest, byte* src, int size)
    {
        const int BatchSize = My128Bit.Size;
        for (; size >= BatchSize; size -= BatchSize)
        {
            *(My128Bit*)dest = *(My128Bit*)src;
            dest += BatchSize;
            src += BatchSize;
        }
        Memcpy64(dest, src, size);
    }

    internal static void Memcpy256(byte* dest, byte* src, int size)
    {
        const int BatchSize = My256Bit.Size;
        for (; size >= BatchSize; size -= BatchSize)
        {
            *(My256Bit*)dest = *(My256Bit*)src;
            dest += BatchSize;
            src += BatchSize;
        }
        Memcpy128(dest, src, size);
    }
    internal static void Memcpy512(byte* dest, byte* src, int size)
    {
        const int BatchSize = My512Bit.Size;
        for (; size >= BatchSize; size -= BatchSize)
        {
            *(My512Bit*)dest = *(My512Bit*)src;
            dest += BatchSize;
            src += BatchSize;
        }
        Memcpy256(dest, src, size);
    }
    internal static void Memcpy1024(byte* dest, byte* src, int size)
    {
        const int BatchSize = My1024Bit.Size;
        for (; size >= BatchSize; size -= BatchSize)
        {
            *(My1024Bit*)dest = *(My1024Bit*)src;
            dest += BatchSize;
            src += BatchSize;
        }
        Memcpy512(dest, src, size);
    }

    internal static void Memcpy512o(byte* dest, byte* src, int size)
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

    // AVX2 - Intel® Core™ i7-10875H (Bluewinds)
    // Memcpy512oTicks 140 ; Memcpy1024Ticks 655 ; Memcpy512Ticks 135 ; Memcpy256Ticks 135 ; Memcpy128Ticks 153 ; Memcpy64Ticks 150

    // AVX2 - AMD 6850U (CptMoore)
    // Memcpy512oTicks 140 ; Memcpy1024Ticks 667 ; Memcpy512Ticks 139 ; Memcpy256Ticks 147 ; Memcpy128Ticks 152 ; Memcpy64Ticks 159

    // AVX512 Double Pump -
    //

    // AVX512 -
    //

    // SSE -
    //

    // should translate to 8x128 ops
    private struct My1024Bit
    {
        internal const int Size = 1024/8;
        internal My512Bit _00;
        internal My512Bit _01;
    }
    // should translate to 4x128 ops
    private struct My512Bit
    {
        internal const int Size = 512/8;
        internal My256Bit _00;
        internal My256Bit _01;
    }
    // should translate to 2x128 ops
    private struct My256Bit
    {
        internal const int Size = 256/8;
        internal My128Bit _00;
        internal My128Bit _01;
    }
    // should translate to xmm 128 op
    private struct My128Bit
    {
        internal const int Size = 128/8;
        internal long _00;
        internal long _01;
    }

    internal static readonly long Memcpy64Ticks = CalcMemcpyTicks(Memcpy64);
    internal static readonly long Memcpy128Ticks = CalcMemcpyTicks(Memcpy128);
    internal static readonly long Memcpy256Ticks = CalcMemcpyTicks(Memcpy256);
    internal static readonly long Memcpy512Ticks = CalcMemcpyTicks(Memcpy512);
    internal static readonly long Memcpy1024Ticks = CalcMemcpyTicks(Memcpy1024);
    internal static readonly long Memcpy512oTicks = CalcMemcpyTicks(Memcpy512o);

    private delegate void Memcpy(byte* dst, byte* src, int size);
    private static long CalcMemcpyTicks(Memcpy memcpy)
    {
        const int MaxSize = 512 * 1024 - 1;
        var srcA = new byte[MaxSize];
        var dstA = new byte[MaxSize];

        const int TestRunsPerSize = 100;
        var memCpyTicks = new long[TestRunsPerSize];

        const int WarmupCount = 1000;
        fixed (byte* src = srcA)
        {
            fixed (byte* dst = dstA)
            {
                for (var w = 0; w < WarmupCount + 1; w++)
                {
                    var shouldMeasure = w == WarmupCount;
                    for (var run = 0; run < TestRunsPerSize; run++)
                    {
                        var start = shouldMeasure ? MTStopwatch.GetTimestamp() : 0;
                        memcpy(dst, src, MaxSize);
                        if (shouldMeasure)
                        {
                            memCpyTicks[run] = MTStopwatch.GetTimestamp() - start;
                        }
                    }
                }
            }
        }
        return MTStopwatch.TicksMin(memCpyTicks);
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