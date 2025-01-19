using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using ModTek.Util.Stopwatch;

namespace ModTek.Features.Logging;

// improve performance by writing bytes instead of chars
internal class FastBuffer
{
    internal FastBuffer(int initialCapacity = 16 * 1024)
    {
        SetCapacity(initialCapacity);
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

    internal void Append(byte value)
    {
        var position = GetPositionAndIncrementLength(1);
        _buffer[position] = value;
    }

    internal void Append(byte[] value)
    {
        var length = value.Length;
        var position = GetPositionAndIncrementLength(length);
        FastSimd.BlockCopy(value, 0, _buffer, position, length);
    }

    internal unsafe void Append(int value)
    {
        var digits = FormattingHelpers.CountDigits((uint)value);
        var position = GetPositionAndIncrementLength(digits);
        fixed (byte* buffer = _buffer)
        {
            FormattingHelpers.WriteDigits(buffer + position, (uint)value, digits);
        }
    }

    internal void Append(string value)
    {
        const int Utf8MaxBytesPerChar = 4;
        EnsureCapacity(_length + value.Length * Utf8MaxBytesPerChar);
        _length += Encoding.UTF8.GetBytes(value, 0, value.Length, _buffer, _length);
    }

    internal void Append(DateTime value)
    {
        AppendTime(value.Hour, value.Minute, value.Second, value.Ticks);
    }

    internal void Append(TimeSpan value)
    {
        AppendTime(value.Hours, value.Minutes, value.Seconds, value.Ticks);
    }

    // unsafe/pointers avoids temporary variables and array bound checks
    private unsafe void AppendTime(int hours, int minutes, int seconds, long ticks)
    {
        var offset = GetPositionAndIncrementLength(17);
        fixed (byte* buffer = _buffer)
        {
            var position = buffer + offset;
            FormattingHelpers.WriteDigits(position, hours, 2);
            position[2] = (byte)':';
            FormattingHelpers.WriteDigits(position + 3, minutes, 2);
            position[5] = (byte)':';
            FormattingHelpers.WriteDigits(position + 6, seconds, 2);
            position[8] = (byte)'.';
            FormattingHelpers.WriteDigits(position + 9, ticks, 7);
            position[16] = (byte)' ';
        }
    }

    private int GetPositionAndIncrementLength(int increment)
    {
        var length = _length;
        var requiredCapacity = _length + increment;
        EnsureCapacity(requiredCapacity);
        _length = requiredCapacity;
        return length;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void EnsureCapacity(int targetLength)
    {
        if (_buffer.Length < targetLength)
        {
            EnlargeCapacity(targetLength);
        }
    }
    internal static readonly MTStopwatch EnlargeCapacityStopWatch = new();
    private void EnlargeCapacity(int targetLength)
    {
        var start = MTStopwatch.GetTimestamp();
        var newLength = Math.Max(3 * _buffer.Length / 2, targetLength);
        SetCapacity(newLength);
        EnlargeCapacityStopWatch.EndMeasurement(start);
    }
    private void SetCapacity(int capacity)
    {
        var newBuffer = new byte[capacity];
        if (_buffer != null)
        {
            Buffer.BlockCopy(_buffer, 0, newBuffer, 0, Math.Min(capacity, _length));
            _buffer = null;
        }
        _buffer = newBuffer;
    }
}