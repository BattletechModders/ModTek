using ModTek.Common.Globals;
using ModTek.Common.Logging;

namespace ModTek.InjectorRunner;

internal class Logger
{
    internal static readonly SimpleLogger Main = new(Paths.InjectorRunnerLogFile);
}
