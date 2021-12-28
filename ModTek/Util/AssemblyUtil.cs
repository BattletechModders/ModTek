using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using static ModTek.Features.Logging.MTLogger;

namespace ModTek.Util
{
    internal static class AssemblyUtil
    {
        private const BindingFlags PUBLIC_STATIC_BINDING_FLAGS = BindingFlags.Public | BindingFlags.Static;

        internal static Assembly LoadDLL(string path)
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
                Log($"\tLoaded assembly {assembly.GetName().Name} (v{assembly.GetName().Version})");
                return assembly;
            }
            catch (Exception e)
            {
                Log($"\t{fileName}: While loading a .dll, an exception occured", e);
                return null;
            }
        }

        internal static IEnumerable<Assembly> GetAssembliesByPattern(string pattern)
        {
            var regex = new Regex(pattern);
            return AppDomain.CurrentDomain.GetAssemblies().Where(a => regex.IsMatch(a.GetName().Name));
        }

        internal static Assembly GetAssemblyByName(string name)
        {
            return AppDomain.CurrentDomain.GetAssemblies().SingleOrDefault(a => a.GetName().Name == name);
        }

        internal static MethodInfo[] FindMethods(Assembly assembly, string methodName, string typeName = null)
        {
            // find types with our method on them
            try
            {
                var types = new List<Type>();
                if (typeName == null)
                {
                    types.AddRange(GetTypesSafe(assembly).Where(x => x.GetMethod(methodName, PUBLIC_STATIC_BINDING_FLAGS) != null));
                }
                else
                {
                    types.Add(assembly.GetType(typeName));
                }

                if (types.Count == 0)
                {
                    return null;
                }

                var methods = new List<MethodInfo>();
                foreach (var type in types)
                {
                    var method = type.GetMethod(methodName, PUBLIC_STATIC_BINDING_FLAGS);
                    methods.Add(method);
                }

                return methods.ToArray();
            }
            catch (Exception e)
            {
                Log($"Can't find method(s) with name {methodName} in assembly {assembly.FullName}", e);
                return null;
            }
        }

        internal static bool InvokeMethodByParameterNames(MethodInfo method, Dictionary<string, object> paramsDictionary)
        {
            var parameterList = new List<object>();
            var methodParameters = method.GetParameters();

            if (methodParameters.Length == 0)
            {
                Log($"\tInvoking '{GetMethodFullName(method)}()' using parameter dictionary");
                method.Invoke(null, null);
                return true;
            }

            var parametersStrings = new List<string>();
            foreach (var parameter in methodParameters)
            {
                var name = parameter.Name;
                if (!paramsDictionary.ContainsKey(name) || paramsDictionary[name].GetType() != parameter.ParameterType)
                {
                    return false;
                }

                parameterList.Add(paramsDictionary[name]);
                parametersStrings.Add($"{parameter.ParameterType.Name} {name}");
            }

            var parametersString = string.Join(", ", parametersStrings.ToArray());
            Log($"\tInvoking '{GetMethodFullName(method)}({parametersString})' using parameter dictionary");
            method.Invoke(null, parameterList.ToArray());
            return true;
        }

        internal static bool InvokeMethodByParameterTypes(MethodInfo method, object[] parameters)
        {
            var methodParameters = method.GetParameters();

            if (parameters == null)
            {
                if (methodParameters.Length != 0)
                {
                    return false;
                }

                Log($"\tInvoking '{GetMethodFullName(method)}()' using parameter type");
                method.Invoke(null, null);
                return true;
            }

            if (parameters.Length != methodParameters.Length)
            {
                return false;
            }

            var parametersStrings = new List<string>();
            for (var i = 0; i < parameters.Length; i++)
            {
                if (parameters[i].GetType() != methodParameters[i].ParameterType)
                {
                    return false;
                }

                parametersStrings.Add($"{methodParameters[i].ParameterType.Name} {methodParameters[i].Name}");
            }

            var parametersString = string.Join(", ", parametersStrings.ToArray());
            Log($"\tInvoking '{GetMethodFullName(method)}({parametersString})' using parameter type");
            method.Invoke(null, parameters);
            return true;
        }

        internal static IEnumerable<Type> GetTypesSafe(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                foreach (var le in e.LoaderExceptions)
                {
                    if (le == null)
                    {
                        continue;
                    }
                    Log($"\tError: Couldn't get all Types from {assembly.GetName()}", le);
                }
                return e.Types.Where(x => x != null);
            }
        }

        internal static string GetAssemblyNameFromMemberInfo(MemberInfo Method)
        {
            return Method.DeclaringType?.Assembly.GetName().Name;
        }

        internal static string GetMethodFullName(MemberInfo Method)
        {
            return Method.DeclaringType?.FullName + "." + Method.Name;
        }

        internal static Type GetTypeByName(string name)
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .Select(a => a.GetType(name))
                .FirstOrDefault(type => type != null);
        }
    }
}
