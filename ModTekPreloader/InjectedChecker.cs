using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace ModTekPreloader
{
    internal static class InjectedChecker
    {
        internal static bool IsInjected(string path)
        {
            using (var game = ModuleDefinition.ReadModule(path))
            {
                return IsModTekInjected(game) || IsBTMLInjected(game) || IsRogueTechPerfFixInjected(game);
            }
        }

        private static bool IsModTekInjected(ModuleDefinition game)
        {
            return game.GetType("BattleTech.Main").Methods.Any(x => x.Name == "LoadModTek");
        }

        private static bool IsBTMLInjected(ModuleDefinition game)
        {
            var searchTypes = new List<TypeDefinition>
            {
                game.GetType("BattleTech.Main"),
                game.GetType("BattleTech.GameInstance")
            };

            foreach (var type in searchTypes)
            {
                // check if btml is attached to any method
                foreach (var methodDefinition in type.Methods)
                {
                    if (IsMethodCalledInMethod(methodDefinition, "System.Void BattleTechModLoader.BTModLoader::Init()"))
                        return true;
                }

                // also have to check in places like IEnumerator generated methods (Nested)
                foreach (var nestedType in type.NestedTypes)
                foreach (var methodDefinition in nestedType.Methods)
                {
                    if (IsMethodCalledInMethod(methodDefinition, "System.Void BattleTechModLoader.BTModLoader::Init()"))
                        return true;
                }
            }

            return false;
        }

        private static bool IsRogueTechPerfFixInjected(ModuleDefinition game)
        {
            return game.GetType("BattleTech.UnityGameInstance").Fields.Any(f => f.Name.StartsWith("RTPFVersion"));
        }

        private static bool IsMethodCalledInMethod(MethodDefinition methodDefinition, string methodSignature)
        {
            if (methodDefinition.Body == null)
                return false;

            foreach (var instruction in methodDefinition.Body.Instructions)
            {
                if (instruction.OpCode.Equals(OpCodes.Call) &&
                    instruction.Operand.ToString().Equals(methodSignature))
                    return true;
            }

            return false;
        }
    }
}
