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

    internal void Unpin()
    {
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
        var position = GetPointerAndIncrementLength(value.Length);
        fixed (byte* bytes = value)
        {
            Memcpy1(position, bytes, value.Length);
        }
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

        // assume one byte per char, enlarge through AppendUsingEncoding if necessary
        EnsureCapacity(_length + processingCount);

        fixed (char* chars = value)
        {
            var positionIterPtr = _bufferPtr + _length;
            var charsIterPtr = chars;

            // loop unrolling similar to Buffer.memcpy1
            // parallelism isn't what makes it particular fast, it's the batching that is helpful (fewer ops overall)

            // 8 is a sweat spot, for large amounts of data: 4 is slower, 16 is slower
            {
                const int IterSize = 8;
                for (; processingCount >= IterSize; processingCount -= IterSize)
                {
                    SetAscii(positionIterPtr, charsIterPtr, 0, out var a0);
                    SetAscii(positionIterPtr, charsIterPtr, 1, out var a1);
                    SetAscii(positionIterPtr, charsIterPtr, 2, out var a2);
                    SetAscii(positionIterPtr, charsIterPtr, 3, out var a3);
                    SetAscii(positionIterPtr, charsIterPtr, 4, out var a4);
                    SetAscii(positionIterPtr, charsIterPtr, 5, out var a5);
                    SetAscii(positionIterPtr, charsIterPtr, 6, out var a6);
                    SetAscii(positionIterPtr, charsIterPtr, 7, out var a7);
                    if (!(
                        a0 &&
                        a1 &&
                        a2 &&
                        a3 &&
                        a4 &&
                        a5 &&
                        a6 &&
                        a7
                    )) {
                        goto Utf8Fallback;
                    }
                    _length += IterSize;
                    positionIterPtr = _bufferPtr + _length;
                    charsIterPtr += IterSize;
                }
            }

            {
                const int IterSize = 2;
                for (; processingCount >= IterSize; processingCount -= IterSize)
                {
                    SetAscii(positionIterPtr, charsIterPtr, 0, out var a0);
                    SetAscii(positionIterPtr, charsIterPtr, 1, out var a1);
                    if (!(
                        a0 &&
                        a1
                    )) {
                        goto Utf8Fallback;
                    }
                    _length += IterSize;
                    positionIterPtr = _bufferPtr + _length;
                    charsIterPtr += IterSize;
                }
            }

            if (processingCount > 0)
            {
                const int IterSize = 1;
                SetAscii(positionIterPtr, charsIterPtr, 0, out var a0);
                if (!a0)
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void SetAscii(byte* positionIterPtr, char* charsIterPtr, int offset, out bool isUnicodeCompatibleAscii)
    {
        var valueAsByte = (byte)charsIterPtr[offset];
        positionIterPtr[offset] = valueAsByte;
        isUnicodeCompatibleAscii = valueAsByte <= 127;
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
        var newHandle = GCHandle.Alloc(newBuffer, GCHandleType.Pinned);
        try
        {
            var newBufferPtr = (byte*)newHandle.AddrOfPinnedObject();

            if (_buffer != null)
            {
                Memcpy1(newBufferPtr, _bufferPtr, _length);
                try
                {
                    _handle.Free();
                }
                catch
                {
                    _buffer = null;
                    _bufferPtr = null;
                }
            }

            _buffer = newBuffer;
            _handle = newHandle;
            _bufferPtr = newBufferPtr;
        }
        catch
        {
            newHandle.Free();
            throw;
        }
    }

    // from Buffer.memcpy1
    private static void Memcpy1(byte* dest, byte* src, int size)
    {
        for (; size >= 8; size -= 8)
        {
            *dest = *src;
            dest[1] = src[1];
            dest[2] = src[2];
            dest[3] = src[3];
            dest[4] = src[4];
            dest[5] = src[5];
            dest[6] = src[6];
            dest[7] = src[7];
            dest += 8;
            src += 8;
        }
        for (; size >= 2; size -= 2)
        {
            *dest = *src;
            dest[1] = src[1];
            dest += 2;
            src += 2;
        }
        if (size <= 0)
            return;
        *dest = *src;
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