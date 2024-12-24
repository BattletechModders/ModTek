using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
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
        var executingAssembly = Assembly.GetExecutingAssembly();
        var applicationDirectory = Path.GetDirectoryName(executingAssembly.Location)!;

        // works on .NET 9 (transparently uses AssemblyLoadContext) and .NET Framework 4.7.2
        var currentDomain = AppDomain.CurrentDomain;
        currentDomain.AssemblyResolve += (_, args) =>
        {
            var resolvingName = new AssemblyName(args.Name);
            var path = Path.Combine(applicationDirectory, resolvingName.Name + ".dll");
            var assembly = Assembly.LoadFrom(path);
            Log.LogMessage(
                $"""
                Requested {args.Name}
                and loaded assembly {assembly.FullName}
                from {path}
                at the behest of {args.RequestingAssembly.FullName}
                {Environment.StackTrace}
                """
            );
            return assembly;
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
        return !Log.HasLoggedErrors;
    }
}