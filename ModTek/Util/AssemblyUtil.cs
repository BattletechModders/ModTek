using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using static ModTek.Util.Logger;

namespace ModTek.Util
{
    internal static class AssemblyUtil
    {
        private const BindingFlags PUBLIC_STATIC_BINDING_FLAGS = BindingFlags.Public | BindingFlags.Static;

        public static Assembly LoadDLL(string path)
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
                LogException($"\t{fileName}: While loading a .dll, an exception occured", e);
                return null;
            }
        }

        public static MethodInfo[] FindMethods(Assembly assembly, string methodName, string typeName = null)
        {
            // find types with our method on them
            try
            {
                var types = new List<Type>();
                if (typeName == null)
                    types.AddRange(assembly.GetTypes().Where(x => x.GetMethod(methodName, PUBLIC_STATIC_BINDING_FLAGS) != null));
                else
                    types.Add(assembly.GetType(typeName));

                if (types.Count == 0)
                    return null;

                var methods = new List<MethodInfo>();
                foreach (var type in types)
                {
                    var method = type.GetMethod(methodName, PUBLIC_STATIC_BINDING_FLAGS);
                    methods.Add(method);
                }

                return methods.ToArray();
            }catch(Exception e)
            {
                LogException($"\t", e);
                return null;
            }
        }

        public static bool InvokeMethodByParameterNames(MethodInfo method, Dictionary<string, object> paramsDictionary)
        {
            var parameterList = new List<object>();
            var methodParameters = method.GetParameters();

            if (methodParameters.Length == 0)
            {
                Log($"\tInvoking '{method.DeclaringType?.Name}.{method.Name}()' using parameter dictionary");
                method.Invoke(null, null);
                return true;
            }

            var parametersStrings = new List<string>();
            foreach (var parameter in methodParameters)
            {
                var name = parameter.Name;
                if (!paramsDictionary.ContainsKey(name) || paramsDictionary[name].GetType() != parameter.ParameterType)
                    return false;

                parameterList.Add(paramsDictionary[name]);
                parametersStrings.Add($"{parameter.ParameterType.Name} {name}");
            }

            var parametersString = string.Join(", ", parametersStrings.ToArray());
            Log($"\tInvoking '{method.DeclaringType?.Name}.{method.Name}({parametersString})' using parameter dictionary");
            method.Invoke(null, parameterList.ToArray());
            return true;
        }

        public static bool InvokeMethodByParameterTypes(MethodInfo method, object[] parameters)
        {
            var methodParameters = method.GetParameters();

            if (parameters == null)
            {
                if (methodParameters.Length != 0)
                    return false;

                Log($"\tInvoking '{method.DeclaringType?.Name}.{method.Name}()' using parameter type");
                method.Invoke(null, null);
                return true;
            }

            if (parameters.Length != methodParameters.Length)
                return false;

            var parametersStrings = new List<string>();
            for (var i = 0; i < parameters.Length; i++)
            {
                if (parameters[i].GetType() != methodParameters[i].ParameterType)
                    return false;

                parametersStrings.Add($"{methodParameters[i].ParameterType.Name} {methodParameters[i].Name}");
            }

            var parametersString = string.Join(", ", parametersStrings.ToArray());
            Log($"\tInvoking '{method.DeclaringType?.Name}.{method.Name}({parametersString})' using parameter type");
            method.Invoke(null, parameters);
            return true;
        }
    }
}
