using ModTek.Common.Globals;
using ModTek.Common.Logging;

namespace ModTek.Preloader;

internal class Logger
{
    internal static readonly SimpleLogger Main = new(Paths.PreloaderLogFile);
}
