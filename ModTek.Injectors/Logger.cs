using ModTek.Common.Globals;
using ModTek.Common.Logging;

namespace ModTek.Injectors;

internal class Logger
{
    internal static readonly SimpleLogger Main = new(Paths.InjectorRunnerLogFile);
}
