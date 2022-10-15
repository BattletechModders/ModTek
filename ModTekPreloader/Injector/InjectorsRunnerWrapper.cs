using System;

namespace ModTekPreloader.Injector
{
    internal class InjectorsRunnerWrapper : MarshalByRefObject
    {
        internal const string ModTekInjectorsDomainName = "ModTekInjectorsDomain";

        // TODO rewrite InjectorsRunner to be MarshalByRefObject itself, then get rid of this class
        internal static void Run()
        {
            // this AppDomain allows us to unload all dlls used by the Preloader and Injectors
            // TODO allow Harmony modifications
            // not supporting modifying Harmony assemblies during injection
            // would need to implement different AppDomains to support Harmony1, 2 and X modifications
            // meaning 3 different injection phases and in between upgrading the shims
            // all the while having to share the assembly cache between the app domains
            var domain = AppDomain.CreateDomain(ModTekInjectorsDomainName);
            try
            {
                var @this = (InjectorsRunnerWrapper)domain.CreateInstance(
                        typeof(InjectorsRunnerWrapper).Assembly.FullName,
                        // ReSharper disable once AssignNullToNotNullAttribute
                        typeof(InjectorsRunnerWrapper).FullName
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
            using (var injectorsRunner = new InjectorsRunner())
            {
                if (injectorsRunner.IsUpToDate)
                {
                    return;
                }

                injectorsRunner.RunInjectors();
                injectorsRunner.SaveToDisk();
            }
        }
    }
}
