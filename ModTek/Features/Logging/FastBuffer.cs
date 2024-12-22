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

    internal void Append(int value)
    {
        var digits = FormattingHelpers.CountDigits((uint)value);
        var position = GetPointerAndIncrementLength(digits);
        FormattingHelpers.WriteDigits(position, (uint)value, digits);
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

        fixed (char* chars = value)
        {
            var positionIterPtr = _bufferPtr + _length;
            var charsIterPtr = chars;

            // loop unrolling similar to Buffer.memcpy1
            // parallelism isn't what makes it particular fast, it's the batching that is helpful (fewer ops overall)

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
                        goto Utf8Fallback;
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
                        goto Utf8Fallback;
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
                    goto Utf8Fallback;
                }
            }

            return;

            Utf8Fallback: // this is 10x slower or more (GetBytes has no fast ASCII path and no SIMD)
            const int Utf8MaxBytesPerChar = 4;
            EnsureCapacity(_length + processingCount * Utf8MaxBytesPerChar);
            var charIndex = value.Length - processingCount;
            _length += Encoding.UTF8.GetBytes(value, charIndex, processingCount, _buffer, _length);
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
        var position = GetPointerAndIncrementLength(17);
        FormattingHelpers.WriteDigits(position, value.Hour, 2);
        position[2] = (byte)':';
        FormattingHelpers.WriteDigits(position + 3, value.Minute, 2);
        position[5] = (byte)':';
        FormattingHelpers.WriteDigits(position + 6, value.Second, 2);
        position[8] = (byte)'.';
        FormattingHelpers.WriteDigits(position + 9, value.Ticks, 7);
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