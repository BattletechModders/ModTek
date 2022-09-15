using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Cecil;

namespace ModTekPreloader.Injector
{
    internal static class AssemblyPublicizer
    {
        private static readonly string[] assembliesToMakePublic =
        {
            "Assembly-CSharp",
            "Assembly-CSharp-firstpass",
            "BattleTech.Common"
        };

        private static readonly string[] typesNotToMakePublic =
        {
            ""
        };

        internal static void MakePublic(IAssemblyResolver resolver, string assembliesPublicizedDirectory)
        {
            Logger.Log("Assemblies publicized:");
            Directory.CreateDirectory(assembliesPublicizedDirectory);
            foreach (var name in assembliesToMakePublic)
            {
                var definition = resolver.Resolve(new AssemblyNameReference(name, null));
                MakePublic(definition);
                var path = Path.Combine(assembliesPublicizedDirectory, $"{definition.Name.Name}.dll");
                Logger.Log($"\t{FileUtils.GetRelativePath(path)}");
                definition.Write(path);
            }
        }

        private static void MakePublic(AssemblyDefinition assembly)
        {
            foreach (var type in GetAllTypes(assembly))
			{
                if (IsCompiledGenerated(type))
                {
                    continue;
                }

                if (type.IsNested)
                {
                    type.IsNestedPublic = true;
                }
                else
                {
                    type.IsPublic = true;
                }

                foreach (var method in type.Methods)
				{
                    if (method.IsCompilerControlled || IsCompiledGenerated(method))
                    {
                        continue;
                    }

                    if (method.IsStatic && method.IsConstructor)
                    {
                        continue;
                    }

                    method.IsPublic = true;
				}

                // property methods are made by the compiler and therefore skipped earlier
                foreach (var property in type.Properties)
                {
                    if (property.GetMethod != null)
                    {
                        property.GetMethod.IsPublic = true;
                    }

                    if (property.SetMethod != null)
                    {
                        property.SetMethod.IsPublic = true;
                    }
                }

				foreach (var field in type.Fields)
				{
					if (field.IsCompilerControlled || IsCompiledGenerated(field))
                    {
                        continue;
                    }

                    field.IsPublic = true;
                    field.IsInitOnly = false;
				}
			}
		}

        private static bool IsCompiledGenerated(ICustomAttributeProvider member)
        {
            return member.CustomAttributes.Any(x => x.AttributeType.FullName == "System.Runtime.CompilerServices.CompilerGeneratedAttribute");
        }

        private static IEnumerable<TypeDefinition> GetAllTypes(AssemblyDefinition assembly)
        {
            var typeQueue = new Stack<TypeDefinition>(assembly.MainModule.Types);

            while (typeQueue.TryPop(out var type))
            {
                if (!typesNotToMakePublic.Contains(type.Name))
                {
                    yield return type;
                }

                foreach (var nestedType in type.NestedTypes)
                {
                    typeQueue.Push(nestedType);
                }
            }
        }
    }
}
