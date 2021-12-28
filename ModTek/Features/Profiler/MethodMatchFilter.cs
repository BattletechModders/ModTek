using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using ModTek.Features.Logging;
using ModTek.Util;
using Newtonsoft.Json;

namespace ModTek.Features.Profiler
{
    internal class MethodMatchFilter
    {
        [JsonProperty]
        [DefaultValue(true)]
        internal bool Enabled = true;

        [JsonProperty(Required = Required.Always)]
        internal string Name;

        [JsonProperty]
        internal string[] ParameterTypeNames;
        [JsonIgnore]
        internal Type[] ParameterTypes;
        private void FillParameterTypes() => ParameterTypes = ParameterTypeNames?.Select(TypeFromName).ToArray();

        [JsonProperty]
        internal string ReturnTypeName;
        [JsonIgnore]
        internal Type ReturnType;
        private void FillReturnType() => ReturnType = ReturnTypeName == null ? null : TypeFromName(ReturnTypeName);

        [JsonProperty]
        internal string ClassTypeName;
        [JsonIgnore]
        internal Type ClassType;
        private void FillClassType() => ClassType = ClassTypeName == null ? null : TypeFromName(ClassTypeName);

        [JsonProperty]
        internal string SubClassOfTypeName;
        [JsonIgnore]
        internal Type SubClassOfType;
        private void FillSubClassOfType() => SubClassOfType = SubClassOfTypeName == null ? null : TypeFromName(SubClassOfTypeName);

        [JsonProperty]
        internal string AssemblyName;
        [JsonIgnore]
        internal Assembly Assembly;
        private void FillAssembly() => Assembly = AssemblyName == null ? null : AssemblyFromName(AssemblyName);

        public override string ToString()
        {
            return "MethodMatchFilter[" +
                $"{nameof(Name)}={Name}" +
                $"; {nameof(ParameterTypeNames)}={ParameterTypeNames?.AsTextList(",")}" +
                $"; {nameof(ReturnTypeName)}={ReturnTypeName}" +
                $"; {nameof(ClassTypeName)}={ClassTypeName}" +
                $"; {nameof(SubClassOfTypeName)}={SubClassOfTypeName}" +
                $"; {nameof(AssemblyName)}={AssemblyName}" +
                "]";
        }

        // should only be called after all (mod) assemblies were loaded
        internal bool FillTypes()
        {
            try
            {
                FillParameterTypes();
                FillReturnType();
                FillClassType();
                FillSubClassOfType();
                FillAssembly();
                return true;
            }
            catch (Exception e)
            {
                MTLogger.Log($"Could find matching data for {this}", e);
                return false;
            }
        }

        private static Type TypeFromName(string name)
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .Select(a => a.GetType(name))
                .First(type => type != null);
        }

        private static Assembly AssemblyFromName(string name)
        {
            var assembly = AssemblyUtil.GetAssemblyByName(name);
            if (assembly == null)
            {
                throw new ArgumentException("Can't find loaded assembly named " + name);
            }
            return assembly;
        }

        internal bool MatchMethod(Assembly assembly, Type type, MethodInfo method, Type[] parameterTypes)
        {
            // method.Name is already checked by caller earlier
            // if (Name != method.Name)
            // {
            //     return false;
            // }

            if (ReturnType != null && ReturnType != method.ReturnType)
            {
                return false;
            }

            if (ClassType != null && ClassType != type)
            {
                return false;
            }

            if (SubClassOfType != null && !type.IsSubclassOf(SubClassOfType))
            {
                return false;
            }

            if (Assembly != null && Assembly != assembly)
            {
                return false;
            }

            if (ParameterTypes != null && !ParameterTypes.SequenceEqual(parameterTypes))
            {
                return false;
            }

            return true;
        }
    }
}
