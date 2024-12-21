using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace ModTek.Features.Logging;

// optimizes writing to a byte array:
// - loop unrolling -> allow some parallelism in the absence of SIMD, AVX could improve this 10x though
// - unsafe -> avoid array bounds checks (cpu branch prediction already skips those, so no gain except code size)
// - ascii conversion first -> quick char to byte conversions if compatible
// - byte based APIs -> avoids unnecessary conversions and allocations if possible
internal unsafe class FastBuffer
{
    internal FastBuffer()
    {
        EnlargeCapacity(16 * 1024);
    }

    private int _length;
    private byte[] _buffer;
    internal int GetBytes(out byte[] bytes)
    {
        bytes = _buffer;
        return _length;
    }

    private GCHandle _handle;
    private byte* _bufferPtr;
    internal void Setup()
    {
        _length = 0;

        if (_handle.IsAllocated)
        {
            return;
        }
        _handle = GCHandle.Alloc(_buffer, GCHandleType.Pinned);
        _bufferPtr = (byte*)_handle.AddrOfPinnedObject();
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

    // mainly used for ThreadId
    internal void Append(int value)
    {
        if (value < 10)
        {
            var position = GetPointerAndIncrementLength(1);
            position[0] = (byte)(value % 10 + AsciiZero);
        }
        else if (value < 100)
        {
            var position = GetPointerAndIncrementLength(2);
            position[0] = (byte)(value / 10 % 10 + AsciiZero);
            position[1] = (byte)(value % 10 + AsciiZero);
        }
        else if (value < 1000)
        {
            var position = GetPointerAndIncrementLength(3);
            position[0] = (byte)(value / 100 % 10 + AsciiZero);
            position[1] = (byte)(value / 10 % 10 + AsciiZero);
            position[2] = (byte)(value % 10 + AsciiZero);
        }
        else
        {
            Append(value.ToString(CultureInfo.InvariantCulture));
        }
    }

    internal void Append(decimal value)
    {
        // TODO optimize
        Append(value.ToString(CultureInfo.InvariantCulture));
    }

    const byte AsciiCompatibleWithUnicodeEqualsOrSmallerThan = 127;
    internal void Append(string value)
    {
        var processingCount = value.Length;
        if (processingCount == 0)
        {
            return;
        }

        // assume one byte per char, enlarge through AppendUsingEncoding if necessary
        EnsureCapacity(_length + processingCount);
        void AppendUsingEncoding(int iterSize)
        {
            const int Utf8MaxBytesPerChar = 4;
            EnsureCapacity(_length + (processingCount - iterSize) + (iterSize * Utf8MaxBytesPerChar));
            var charIndex = value.Length - processingCount;
            _length += Encoding.UTF8.GetBytes(value, charIndex, iterSize, _buffer, _length);
        }

        fixed (char* chars = value)
        {
            var positionIterPtr = _bufferPtr + _length;
            var charsIterPtr = chars;

            // loop unrolling similar to Buffer.memcpy1

            {
                const int IterSize = 8;
                for (; processingCount >= IterSize; processingCount -= IterSize)
                {
                    var r0 = SetAscii(positionIterPtr, charsIterPtr, 0);
                    var r1 = SetAscii(positionIterPtr, charsIterPtr, 1);
                    var r2 = SetAscii(positionIterPtr, charsIterPtr, 2);
                    var r3 = SetAscii(positionIterPtr, charsIterPtr, 3);
                    var r4 = SetAscii(positionIterPtr, charsIterPtr, 4);
                    var r5 = SetAscii(positionIterPtr, charsIterPtr, 5);
                    var r6 = SetAscii(positionIterPtr, charsIterPtr, 6);
                    var r7 = SetAscii(positionIterPtr, charsIterPtr, 7);
                    if (r0 && r1 && r2 && r3 && r4 && r5 && r6 && r7)
                    {
                        _length += IterSize;
                    }
                    else
                    {
                        AppendUsingEncoding(IterSize);
                    }
                    positionIterPtr = _bufferPtr + _length;
                    charsIterPtr += IterSize;
                }
            }

            {
                const int IterSize = 2;
                for (; processingCount >= IterSize; processingCount -= IterSize)
                {
                    var r0 = SetAscii(positionIterPtr, charsIterPtr, 0);
                    var r1 = SetAscii(positionIterPtr, charsIterPtr, 1);
                    if (r0 && r1)
                    {
                        _length += IterSize;
                    }
                    else
                    {
                        AppendUsingEncoding(IterSize);
                    }
                    positionIterPtr = _bufferPtr + _length;
                    charsIterPtr += IterSize;
                }
            }

            if (processingCount > 0)
            {
                const int IterSize = 1;
                var r0 = SetAscii(positionIterPtr, charsIterPtr, 0);
                if (r0)
                {
                    _length += IterSize;
                }
                else
                {
                    AppendUsingEncoding(IterSize);
                }
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool SetAscii(byte* positionIterPtr, char* charsIterPtr, int offset)
    {
        var valueAsByte = (byte)charsIterPtr[offset];
        positionIterPtr[offset] = valueAsByte;
        return valueAsByte <= AsciiCompatibleWithUnicodeEqualsOrSmallerThan;
    }

    internal void Append(DateTime value)
    {
        var hour = value.Hour;
        var minute = value.Minute;
        var second = value.Second;
        var ticks = value.Ticks;

        var position = GetPointerAndIncrementLength(17);
        position[0] = (byte)(hour / 10 % 10 + AsciiZero);
        position[1] = (byte)(hour % 10 + AsciiZero);
        position[2] = (byte)':';
        position[3] = (byte)(minute / 10 % 10 + AsciiZero);
        position[4] = (byte)(minute % 10 + AsciiZero);
        position[5] = (byte)':';
        position[6] = (byte)(second / 10 % 10 + AsciiZero);
        position[7] = (byte)(second % 10 + AsciiZero);
        position[8] = (byte)'.';
        position[9] = (byte)(ticks / 1_000_000 % 10 + AsciiZero);
        position[10] = (byte)(ticks / 100_000 % 10 + AsciiZero);
        position[11] = (byte)(ticks / 10_000 % 10 + AsciiZero);
        position[12] = (byte)(ticks / 1_000 % 10 + AsciiZero);
        position[13] = (byte)(ticks / 100 % 10 + AsciiZero);
        position[14] = (byte)(ticks / 10 % 10 + AsciiZero);
        position[15] = (byte)(ticks % 10 + AsciiZero);
        position[16] = (byte)' ';
    }
    const byte AsciiZero = (byte)'0';

    //[MethodImpl(MethodImplOptions.AggressiveInlining)]
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