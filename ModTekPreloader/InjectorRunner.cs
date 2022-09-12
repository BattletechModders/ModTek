using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace ModTekPreloader
{
    public class InjectorRunner : MarshalByRefObject
    {
        public void RunInjectors(DateTime loggerStart)
        {
            Logger.Setup(loggerStart);

            using (var cache = new AssemblyCache())
            {
                var paths = new Paths();
                var parameters = new object[]
                {
                    cache
                };

                var originalConsoleOut = Console.Out;
                var originalConsoleError = Console.Error;

                Logger.Log($"Searching injector dlls.");
                foreach (var injectorPath in Directory.GetFiles(paths.injectorsDirectory, "*.dll").OrderBy(p => p))
                {
                    Logger.Log($"Running injector {Path.GetFileName(injectorPath)}.");
                    var injector = Assembly.LoadFile(injectorPath);
                    foreach (var injectMethod in injector
                                 .GetTypes()
                                 .Where(t => t.Name == "Injector")
                                 .Select(t => t.GetMethod("Inject", BindingFlags.Public | BindingFlags.Static))
                                 .Where(m => m != null))
                    {
                        var name = injector.GetName().Name;

                        using (var errorLogger = new ConsoleLogger { Prefix = $"{name} Error: " })
                        using (var infoLogger = new ConsoleLogger { Prefix = $"{name}: " })
                        {
                            Console.SetOut(infoLogger);
                            Console.SetError(errorLogger);
                            try
                            {
                                injectMethod.Invoke(null, parameters);
                            }
                            finally
                            {
                                Console.SetOut(originalConsoleOut);
                                Console.SetError(originalConsoleError);
                            }
                        }
                        break;
                    }
                }

                cache.SaveAssembliesToDisk(paths.assembliesInjectedDirectory);
            }
        }

        private class ConsoleLogger : TextWriter
        {
            public string Prefix { get; set; } = string.Empty;
            public override Encoding Encoding => Encoding.UTF8;

            private readonly StringBuilder buffer = new StringBuilder();

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
    }
}
