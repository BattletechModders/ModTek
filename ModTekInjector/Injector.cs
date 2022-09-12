using System;
using System.IO;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace ModTekInjector
{
    internal static class Injector
    {
        private const string MODTEK_DLL_FILE_NAME = "ModTek.dll";
        private const string MODTEK_INIT_TYPE = "ModTek.ModTek";
        private const string MODTEK_INIT_METHOD = "Init";

        private const string GAME_HOOK_TYPE = "BattleTech.Main";
        private const string GAME_HOOK_METHOD = "Start";

        public static void Inject(IAssemblyResolver resolver)
        {
            Console.WriteLine($"Injecting call to {MODTEK_INIT_TYPE}.{MODTEK_INIT_METHOD} from {GAME_HOOK_TYPE}.{GAME_HOOK_METHOD}");

            var gameDirectory = Directory.GetCurrentDirectory();
            var modsDirectory = Path.Combine(gameDirectory, "Mods");
            var modTekDirectory = Path.Combine(modsDirectory, "ModTek");
            var modTekDLLPath = Path.Combine(modTekDirectory, MODTEK_DLL_FILE_NAME);

            var game = resolver.Resolve(new AssemblyNameReference("Assembly-CSharp", null));
            using (var modtek = AssemblyDefinition.ReadAssembly(modTekDLLPath))
            {
                InjectCall(game, modtek);
            }
        }

        private static void InjectCall(AssemblyDefinition game, AssemblyDefinition modtek)
        {
            // get the methods that we're hooking and injecting
            var hookType = game.MainModule.GetType(GAME_HOOK_TYPE);
            var methods = hookType.Methods;
            var injectedMethod = modtek.MainModule.GetType(MODTEK_INIT_TYPE).Methods.Single(x => x.Name == MODTEK_INIT_METHOD);
            var hookedMethod = methods.First(x => x.Name == GAME_HOOK_METHOD);

            // if the return type is an iterator -- need to go searching for its MoveNext method which contains the actual code you'll want to inject
            if (hookedMethod.ReturnType.Name.Equals("IEnumerator"))
            {
                var nestedIterator = hookType.NestedTypes.First(x => x.Name.Contains(GAME_HOOK_METHOD));
                hookedMethod = nestedIterator.Methods.First(x => x.Name.Equals("MoveNext"));
            }

            // As of BattleTech v1.1 the Start() iterator method of BattleTech.Main has this at the end
            //  ...
            //  Serializer.PrepareSerializer();
            //  this.activate.enabled = true;
            //  yield break;
            //}

            // we want to inject after the PrepareSerializer call -- so search for that call in the CIL
            var targetInstruction = -1;
            for (var i = 0; i < hookedMethod.Body.Instructions.Count; i++)
            {
                var instruction = hookedMethod.Body.Instructions[i];

                if (!instruction.OpCode.Code.Equals(Code.Call) || !instruction.OpCode.OperandType.Equals(OperandType.InlineMethod))
                    continue;

                var methodReference = (MethodReference)instruction.Operand;
                if (methodReference.Name.Contains("PrepareSerializer"))
                    targetInstruction = i;
            }

            if (targetInstruction == -1)
                throw new Exception("Couldn't find anything");

            hookedMethod.Body.GetILProcessor().InsertAfter(hookedMethod.Body.Instructions[targetInstruction],
                Instruction.Create(OpCodes.Call, game.MainModule.ImportReference(injectedMethod)));
        }
    }
}