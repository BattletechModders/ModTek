using System.IO;
using System.Text;

namespace ModTekPreloader.Logging;

internal class ConsoleLoggerAdapter : TextWriter
{
    public string Prefix { get; set; } = string.Empty;
    public override Encoding Encoding => Encoding.UTF8;

    private readonly StringBuilder buffer = new();

    public override void Write(char value)
    {
        if (value == '\n')
        {
            Flush();
        }
        else if (value == '\r')
        {
            // ignore
        }
        else
        {
            buffer.Append(value);
        }
    }

    public override void Flush()
    {
        Logger.Log(Prefix + buffer);
        buffer.Clear();
    }

    protected override void Dispose(bool disposing)
    {
        if (!disposing)
        {
            return;
        }

        if (buffer.Length > 0)
        {
            Flush();
        }
    }
}