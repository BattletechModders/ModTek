using System;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace ModTekInjector;

internal static class Injector
{
    private const string MODTEK_INIT_TYPE = "ModTek.ModTek";
    private const string MODTEK_INIT_METHOD = "Init";

    private const string GAME_HOOK_TYPE = "BattleTech.Main";
    private const string GAME_HOOK_METHOD = "Start";

    public static void Inject(IAssemblyResolver resolver)
    {
        Console.WriteLine($"Injecting call to {MODTEK_INIT_TYPE}.{MODTEK_INIT_METHOD} from {GAME_HOOK_TYPE}.{GAME_HOOK_METHOD}");

        var game = resolver.Resolve(new AssemblyNameReference("Assembly-CSharp", null));
        var modtek = resolver.Resolve(new AssemblyNameReference("ModTek", null));
        InjectCall(game, modtek);
    }

    private static void InjectCall(AssemblyDefinition game, AssemblyDefinition modtek)
    {
        // get the methods that we're hooking and injecting
        var hookType = game.MainModule.GetType(GAME_HOOK_TYPE);
        var methods = hookType.Methods;
        var modTekInitMethod = modtek.MainModule.GetType(MODTEK_INIT_TYPE).Methods.Single(x => x.Name == MODTEK_INIT_METHOD);
        var hookMethod = methods.First(x => x.Name == GAME_HOOK_METHOD);

        // if the return type is an iterator -- need to go searching for its MoveNext method which contains the actual code you'll want to inject
        if (hookMethod.ReturnType.Name.Equals("IEnumerator"))
        {
            hookMethod = hookType
                .NestedTypes
                .First(x => x.Name.Contains(GAME_HOOK_METHOD))
                .Methods
                .First(x => x.Name.Equals("MoveNext"));
        }

        // As of BattleTech v1.1 the Start() iterator method of BattleTech.Main has this at the end
        //  ...
        //  Serializer.PrepareSerializer();
        //  this.activate.enabled = true;
        //  yield break;
        //}

        Instruction FindPrepareSerializerCall()
        {
            // we want to inject after the PrepareSerializer call -- so search for that call in the CIL
            foreach (var instruction in hookMethod.Body.Instructions)
            {
                var opcode = instruction.OpCode;
                if (!opcode.Code.Equals(Code.Call))
                {
                    continue;
                }
                if (!opcode.OperandType.Equals(OperandType.InlineMethod))
                {
                    continue;
                }
                var methodReference = (MethodReference)instruction.Operand;
                if (methodReference.Name != "PrepareSerializer")
                {
                    continue;
                }

                return instruction;
            }

            throw new Exception("Couldn't find Serializer.PrepareSerializer");
        }

        var existingInstruction = FindPrepareSerializerCall();
        var modTekInitInstruction = Instruction.Create(
            OpCodes.Call,
            game.MainModule.ImportReference(modTekInitMethod)
        );

        hookMethod.Body.GetILProcessor().InsertAfter(existingInstruction, modTekInitInstruction);
    }
}