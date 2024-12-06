using System;
using System.Text;

namespace ModTek.Features.Logging;

internal class FastBuffer
{
    private int _position;
    private byte[] _buffer = new byte[4 * 1024];

    internal void Clear()
    {
        _position = 0;
    }

    internal int GetBytes(out byte[] bytes)
    {
        bytes = _buffer;
        return _position;
    }

    private readonly char[] _chars = new char[1];
    internal void Append(char value)
    {
        const int Utf8MaxBytesPerChar = 4;
        if (_buffer.Length < _position + Utf8MaxBytesPerChar)
        {
            Array.Resize(ref _buffer, 2 * _buffer.Length);
        }

        if (value <= 'ÿ') // char fits in a single byte, see Convert.ToByte(char)
        {
            var valueAsByte = (byte)value; // the code below is twice as fast with byte instead of char

            // filter out most control characters
            // https://en.wikipedia.org/wiki/Unicode_control_characters
            // const byte C0Start = 0x00;
            const byte C0End = 0x1F;
            if (valueAsByte <= C0End)
            {
                const byte HT = (byte)'\t';
                const byte LF = (byte)'\n';
                const byte CR = (byte)'\r';
                switch (valueAsByte)
                {
                    case HT:
                    case LF:
                    case CR:
                        break;
                    default:
                        return;
                }
            }
            else
            {
                const byte C1Start = 0x7F;
                const byte C1End = 0x9F;
                if (valueAsByte is >= C1Start and <= C1End)
                {
                    return;
                }
            }

            const byte AsciiCompatibleWithUnicodeBelow = 127;
            if (valueAsByte <= AsciiCompatibleWithUnicodeBelow)
            {
                _buffer[_position++] = valueAsByte;
                return;
            }
        }

        _chars[0] = value;
        _position += Encoding.UTF8.GetBytes(_chars, 0, 1, _buffer, _position);
    }

    internal void Append(string value)
    {
        foreach (var c in value)
        {
            Append(c);
        }
    }

    internal void AppendLast2Digits(long value)
    {
        Append((char)(value / 10 + '0'));
        Append((char)(value % 10 + '0'));
    }

    internal void AppendLast7Digits(long value)
    {
        Append((char)(((value / 1_000_000) % 10) + '0'));
        Append((char)(((value / 100_000) % 10) + '0'));
        Append((char)(((value / 10_000) % 10) + '0'));
        Append((char)(((value / 1_000) % 10) + '0'));
        Append((char)(((value / 100) % 10) + '0'));
        Append((char)(((value / 10) % 10) + '0'));
        Append((char)(((value / 1) % 10) + '0'));
    }
}