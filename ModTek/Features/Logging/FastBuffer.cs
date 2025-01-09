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
                Memcpy256(position, bytes, value.Length);
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
                        Memcpy256(dst, bytes, size);
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
            var srcPtr = (byte*)chars;

            // parallelism isn't what makes it particular fast, it's the batching that is helpful (fewer ops overall)
            // 8 is a sweat spot, since we can do the ASCII bit mask check with an ulong
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
                        goto Utf8Fallback;
                    }
                    dstPtr += IterSize;
                    srcPtr += 2*IterSize;
                    _length += IterSize;
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
                        goto Utf8Fallback;
                    }
                    dstPtr += IterSize;
                    srcPtr += 2*IterSize;
                    _length += IterSize;
                }
            }

            if (processingCount > 0)
            {
                const int IterSize = 1;
                *(dstPtr + 0) = *(srcPtr + 0 * 2);

                const byte NonAsciiBitmask = 1 << 7;
                if ((*dstPtr & NonAsciiBitmask) != 0)
                {
                    goto Utf8Fallback;
                }
                _length += IterSize;
            }

            return;

            Utf8Fallback: // this is 10x slower or more (GetBytes has no fast ASCII path and no SIMD in this old .NET)
            var measurement = MTStopwatch.GetTimestamp();
            var charIndex = value.Length - processingCount;
            const int Utf8MaxBytesPerChar = 4;
            EnsureCapacity(_length + processingCount * Utf8MaxBytesPerChar);
            _length += Encoding.UTF8.GetBytes(value, charIndex, processingCount, _buffer, _length);
            UTF8FallbackStopwatch.EndMeasurement(measurement);
        }
    }
    internal static readonly MTStopwatch UTF8FallbackStopwatch = new();

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

    // from Buffer.memcpy* and optimized to use wider types like 128 and 256 bit
    // JIT can do xmm (128) and cpu can optimize 2x xmm (2x128) further it seems
    internal static void Memcpy256(byte* dest, byte* src, int size)
    {
        { // 25% faster than if using 2x128 on AMD Zen4 hardware
            const int BatchSize = My256Bit.Size;
            for (; size >= BatchSize; size -= BatchSize)
            {
                *(My256Bit*)dest = *(My256Bit*)src;
                dest += BatchSize;
                src += BatchSize;
            }
        }
        { // 100% faster than if using 2x64 on xmm hardware
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

    // the jit can optimize this to 2x xmm 128 ops
    // and 2x 128bit ops together are 25% faster than looping over 128bit ops
    private struct My128Bit
    {
        internal const int Size = 128/8;
        internal long _00;
        internal long _01;
    }
    private struct My256Bit
    {
        internal const int Size = 256/8;
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