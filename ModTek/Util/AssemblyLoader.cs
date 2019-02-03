using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace ModTek.Util
{
    using static Logger;

    internal static class AssemblyLoader
    {
        private const BindingFlags PUBLIC_STATIC_BINDING_FLAGS = BindingFlags.Public | BindingFlags.Static;

        public static Assembly LoadDLL(string path, string methodName = "Init", string typeName = null,
            object[] parameters = null, BindingFlags bFlags = PUBLIC_STATIC_BINDING_FLAGS)
        {
            var fileName = Path.GetFileName(path);

            if (!File.Exists(path))
            {
                Log($"\tFailed to load {fileName} at path {path}, because it doesn't exist at that path.");
                return null;
            }

            try
            {
                var assembly = Assembly.LoadFrom(path);
                var name = assembly.GetName();
                var version = name.Version;
                var types = new List<Type>();

                // if methodName is null, don't try to run an entry point
                if (methodName == null)
                    return assembly;

                // find the type/s with our entry point/s
                if (typeName == null)
                {
                    types.AddRange(assembly.GetTypes().Where(x => x.GetMethod(methodName, bFlags) != null));
                }
                else
                {
                    types.Add(assembly.GetType(typeName));
                }

                if (types.Count == 0)
                {
                    Log($"\t{fileName} (v{version}): Failed to find specified entry point: {typeName ?? "NotSpecified"}.{methodName}");
                    return null;
                }

                // run each entry point
                foreach (var type in types)
                {
                    var entryMethod = type.GetMethod(methodName, bFlags);
                    var methodParams = entryMethod?.GetParameters();

                    if (methodParams == null)
                        continue;

                    if (methodParams.Length == 0)
                    {
                        Log($"\t{fileName} (v{version}): Found and called entry point \"{entryMethod}\" in type \"{type.FullName}\"");
                        entryMethod.Invoke(null, null);
                        continue;
                    }

                    // match up the passed in params with the method's params, if they match, call the method
                    if (parameters != null && methodParams.Length == parameters.Length
                        && !methodParams.Where((info, i) => parameters[i]?.GetType() != info.ParameterType).Any())
                    {
                        Log($"\t{fileName} (v{version}): Found and called entry point \"{entryMethod}\" in type \"{type.FullName}\"");
                        entryMethod.Invoke(null, parameters);
                        continue;
                    }

                    // failed to call entry method of parameter mismatch
                    // diagnosing problems of this type is pretty hard
                    Log($"\t{fileName} (v{version}): Provided params don't match {type.Name}.{entryMethod.Name}");
                    Log("\t\tPassed in Params:");
                    if (parameters != null)
                    {
                        foreach (var parameter in parameters)
                            Log($"\t\t\t{parameter.GetType()}");
                    }
                    else
                    {
                        Log("\t\t\t'parameters' is null");
                    }

                    if (methodParams.Length != 0)
                    {
                        Log("\t\tMethod Params:");
                        foreach (var prm in methodParams)
                            Log($"\t\t\t{prm.ParameterType}");
                    }
                }

                return assembly;
            }
            catch (Exception e)
            {
                LogException($"\t{fileName}: While loading a dll, an exception occured", e);
                return null;
            }
        }
    }
}
