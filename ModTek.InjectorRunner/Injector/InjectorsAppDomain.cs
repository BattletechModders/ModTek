using System;

namespace ModTek.InjectorRunner.Injector;

internal class InjectorsAppDomain : MarshalByRefObject
{
    private const string ModTekInjectorsDomainName = "ModTekInjectorsDomain";

    internal static void Run()
    {
        // this AppDomain allows us to unload all dlls used by the Injectors
        // also it enforces isolation to allow running injectors during project build time
        var domain = AppDomain.CreateDomain(ModTekInjectorsDomainName);
        try
        {
            var @this = (InjectorsAppDomain)domain.CreateInstance(
                    typeof(InjectorsAppDomain).Assembly.FullName,
                    // ReSharper disable once AssignNullToNotNullAttribute
                    typeof(InjectorsAppDomain).FullName
                )
                .Unwrap();

            @this.RunInjectors();
        }
        finally
        {
            AppDomain.Unload(domain);
        }
    }

    private void RunInjectors()
    {
        using var injectorsRunner = new InjectorsRunner();
        if (injectorsRunner.IsUpToDate)
        {
            return;
        }

        injectorsRunner.RunInjectors();
        injectorsRunner.SaveToDisk();
    }
}