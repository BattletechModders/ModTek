using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Versioning;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using ModTek.Injectors;

namespace ModTek.InjectorsTask;

/*
 * see https://learn.microsoft.com/en-us/visualstudio/msbuild/tutorial-custom-task-code-generation?view=vs-2022#generate-a-console-app-and-use-the-custom-task
 * see https://github.com/krafs/Publicizer/blob/main/src/Publicizer/Krafs.Publicizer.targets and props
 * - run before Rafs.Publicizer
 *   - needs to modify the references? or just push them ahead so they are found?
 *   - see Rafs ReferencePathsToDelete and ReferencePathsToAdd and ReferencePaths etc....
 */
public class ModTekInjectorsRunner : Task
{
    [Required]
    public ITaskItem[] ReferencePaths { get; set; } = null!;

    [Output]
    public ITaskItem[] ReferencePathsToDelete { get; set; } = [];

    [Output]
    public ITaskItem[] ReferencePathsToAdd { get; set; } = [];

    public override bool Execute()
    {
        try
        {
            ExecuteInternal();
        }
        catch (Exception ex)
        {
            Log.LogWarning($"{Environment.OSVersion} ; CLR {Environment.Version} ; {System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription}");
            Log.LogErrorFromException(ex, true, true, null);

            var baseLocation = Path.GetFullPath(Path.Combine(Assembly.GetExecutingAssembly().Location, "..", "..", "..", ".."));
            var assemblies = AppDomain.CurrentDomain.GetAssemblies()
                .OrderBy(assembly => assembly.GetName().Name)
                .Where(assembly => Path.GetFullPath(assembly.Location).StartsWith(baseLocation));
            foreach (var assembly in assemblies)
            {
                var targetFramework = assembly.GetCustomAttribute(typeof(TargetFrameworkAttribute)) as TargetFrameworkAttribute;
                Log.LogWarning($"{targetFramework?.FrameworkDisplayName ?? "??"} {assembly.GetName().Name} {assembly.GetName().Version} {assembly.Location}");
            }
        }
        return !Log.HasLoggedErrors;
    }

    private void ExecuteInternal()
    {
        var executingAssembly = Assembly.GetExecutingAssembly();
        var applicationDirectory = Path.GetDirectoryName(executingAssembly.Location)!;

        // works on .NET 9 (transparently uses AssemblyLoadContext) and .NET Framework 4.7.2
        var currentDomain = AppDomain.CurrentDomain;
        currentDomain.AssemblyResolve += (_, args) =>
        {
            var requestingName = args.RequestingAssembly.FullName;
            var resolvingName = new AssemblyName(args.Name);

            // sometimes assemblies loaded without a public token can't resolve against requests that require a public token
            // so lets compare only the name
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies().Where(a => resolvingName.Name == a.GetName().Name))
            {
                Log.LogMessage($"Returning already loaded assembly {assembly.Location} for {requestingName}");
                return assembly;
            }

            // if we can provide the missing dll, return it
            var path = Path.Combine(applicationDirectory, resolvingName.Name + ".dll");
            if (File.Exists(path))
            {
                Log.LogMessage($"Loading assembly {path} for {requestingName}");
                var assembly = Assembly.LoadFrom(path);
                return assembly;
            }

            // not good
            Log.LogWarning($"Could not find assembly {resolvingName.Name} for {requestingName}");
            return null;
        };

        Runner.Run();

        var referencePathsToAdd = new List<ITaskItem>();
        var referencePathsToDelete = new List<ITaskItem>();
        foreach (var path in Runner.GetInjectedPaths())
        {
            var filename = Path.GetFileNameWithoutExtension(path);
            foreach (var referencePath in ReferencePaths)
            {
                if (filename == referencePath.FileName())
                {
                    var newReference = new TaskItem(path);
                    referencePathsToAdd.Add(newReference);
                    referencePathsToDelete.Add(referencePath);
                    break;
                }
            }
        }
        ReferencePathsToDelete = referencePathsToDelete.ToArray();
        ReferencePathsToAdd = referencePathsToAdd.ToArray();
    }
}