using System;
using ModTek.Common.Globals;
using ModTek.InjectorRunner.Injector;

namespace ModTek.Preloader;

internal class InjectorsAppDomain : MarshalByRefObject
{
    private const string ModTekInjectorsDomainName = "ModTekInjectorsDomain";

    internal static void Run()
    {
        // this AppDomain allows us to unload all dlls used by the Injectors
        // also it enforces isolation to allow running injectors during project build time
        var domain = AppDomain.CreateDomain(ModTekInjectorsDomainName, null, new AppDomainSetup
        {
            // for some reason InjectorRunner can't find itself within the newly created AppDomain otherwise
            PrivateBinPath = Paths.ModTekLibDirectory
        });
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
        InjectorsRunner.Run();
    }
}