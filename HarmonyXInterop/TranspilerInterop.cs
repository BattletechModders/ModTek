#region LICENSE

/*MIT License

Copyright (c) 2017 Andreas Pardeike

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

#endregion

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using MonoMod.Utils;

namespace HarmonyXInterop
{
    internal static class TranspilerInterop
    {
        private static readonly Dictionary<OpCode, OpCode> AllJumpCodes = new Dictionary<OpCode, OpCode>
        {
            {OpCodes.Beq_S, OpCodes.Beq},
            {OpCodes.Bge_S, OpCodes.Bge},
            {OpCodes.Bge_Un_S, OpCodes.Bge_Un},
            {OpCodes.Bgt_S, OpCodes.Bgt},
            {OpCodes.Bgt_Un_S, OpCodes.Bgt_Un},
            {OpCodes.Ble_S, OpCodes.Ble},
            {OpCodes.Ble_Un_S, OpCodes.Ble_Un},
            {OpCodes.Blt_S, OpCodes.Blt},
            {OpCodes.Blt_Un_S, OpCodes.Blt_Un},
            {OpCodes.Bne_Un_S, OpCodes.Bne_Un},
            {OpCodes.Brfalse_S, OpCodes.Brfalse},
            {OpCodes.Brtrue_S, OpCodes.Brtrue},
            {OpCodes.Br_S, OpCodes.Br},
            {OpCodes.Leave_S, OpCodes.Leave}
        };

        private static readonly Dictionary<MethodInfo, MethodInfo> Wrappers = new Dictionary<MethodInfo, MethodInfo>();

        private static readonly MethodInfo ResolveToken = AccessTools.Method(typeof(MethodBase),
            nameof(MethodBase.GetMethodFromHandle), new[] {typeof(RuntimeMethodHandle)});

        private static readonly MethodInfo ApplyTranspilerMethod =
            AccessTools.Method(typeof(TranspilerInterop), nameof(ApplyTranspiler));

        public static MethodInfo WrapInterop(MethodInfo transpiler)
        {
            lock (Wrappers)
            {
                if (Wrappers.TryGetValue(transpiler, out var wrapped))
                    return wrapped;
            }

            using (var dmd = new DynamicMethodDefinition($"TranspilerWrapper<{transpiler.GetID(simple: true)}>",
                typeof(IEnumerable<CodeInstruction>), new[]
                {
                    typeof(IEnumerable<CodeInstruction>),
                    typeof(ILGenerator),
                    typeof(MethodBase)
                }))
            {
                var il = dmd.GetILGenerator();
                il.Emit(OpCodes.Ldtoken, transpiler);
                il.Emit(OpCodes.Call, ResolveToken);
                il.Emit(OpCodes.Castclass, typeof(MethodInfo));
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Ldarg_2);
                il.Emit(OpCodes.Call, ApplyTranspilerMethod);
                il.Emit(OpCodes.Ret);

                var generatedWrapper = dmd.GenerateWith<DMDCecilGenerator>();

                lock (Wrappers)
                {
                    Wrappers[transpiler] = generatedWrapper;
                }

                return generatedWrapper;
            }
        }

        private static IEnumerable<CodeInstruction> ApplyTranspiler(MethodInfo transpiler,
            IEnumerable<CodeInstruction> instructions, ILGenerator generator, MethodBase method)
        {
            var tempInstructions = ConvertToGeneralInstructions(transpiler, instructions, out var unassignedValues);

            List<object> originalInstructions = null;
            if (unassignedValues is object)
                originalInstructions = instructions.Cast<object>().ToList();
            var parameter = GetTranspilerCallParameters(generator, transpiler, method, tempInstructions);
            var newInstructions = transpiler.Invoke(null, parameter.ToArray()) as IEnumerable;
            if (newInstructions is object)
                tempInstructions = newInstructions;

            if (unassignedValues is object)
                tempInstructions = ConvertToOurInstructions(tempInstructions, typeof(CodeInstruction),
                    originalInstructions, unassignedValues);

            return tempInstructions as List<CodeInstruction> ?? tempInstructions.Cast<CodeInstruction>().ToList();
        }

        private static OpCode ReplaceShortJumps(OpCode opcode)
        {
            foreach (var pair in AllJumpCodes.Where(pair => opcode == pair.Key))
                return pair.Value;
            return opcode;
        }

        private static object ConvertInstruction(Type type, object instruction,
            out Dictionary<string, object> unassigned)
        {
            var nonExisting = new Dictionary<string, object>();
            var elementTo = AccessTools.MakeDeepCopy(instruction, type, (namePath, trvSrc, trvDest) =>
            {
                var value = trvSrc.GetValue();

                if (!(trvDest.FieldExists() is false))
                    return namePath == nameof(CodeInstruction.opcode) ? ReplaceShortJumps((OpCode) value) : value;
                nonExisting[namePath] = value;
                return null;
            });
            unassigned = nonExisting;
            return elementTo;
        }

        private static bool ShouldAddExceptionInfo(object op, int opIndex, List<object> originalInstructions,
            List<object> newInstructions, Dictionary<object, Dictionary<string, object>> unassignedValues)
        {
            var originalIndex = originalInstructions.IndexOf(op);
            if (originalIndex == -1)
                return false; // no need, new instruction

            if (unassignedValues.TryGetValue(op, out var unassigned) is false)
                return false; // no need, no unassigned info

            if (unassigned.TryGetValue(nameof(CodeInstruction.blocks), out var blocksObject) is false)
                return false; // no need, no try-catch info
            var blocks = blocksObject as List<ExceptionBlock>;

            var dupCount = newInstructions.Count(instr => instr == op);
            if (dupCount <= 1)
                return true; // ok, no duplicate found

            var isStartBlock = blocks.FirstOrDefault(block => block.blockType != ExceptionBlockType.EndExceptionBlock);
            var isEndBlock = blocks.FirstOrDefault(block => block.blockType == ExceptionBlockType.EndExceptionBlock);

            if (isStartBlock != null && isEndBlock is null)
            {
                var pairInstruction = originalInstructions.Skip(originalIndex + 1).FirstOrDefault(instr =>
                {
                    if (unassignedValues.TryGetValue(instr, out unassigned) is false)
                        return false;
                    if (unassigned.TryGetValue(nameof(CodeInstruction.blocks), out blocksObject) is false)
                        return false;
                    blocks = blocksObject as List<ExceptionBlock>;
                    return blocks.Any();
                });
                if (pairInstruction != null)
                {
                    var pairStart = originalIndex + 1;
                    var pairEnd = pairStart + originalInstructions.Skip(pairStart).ToList().IndexOf(pairInstruction) -
                                  1;
                    var originalBetweenInstructions = originalInstructions
                        .GetRange(pairStart, pairEnd - pairStart)
                        .Intersect(newInstructions);

                    pairInstruction = newInstructions.Skip(opIndex + 1).FirstOrDefault(instr =>
                    {
                        if (unassignedValues.TryGetValue(instr, out unassigned) is false)
                            return false;
                        if (unassigned.TryGetValue(nameof(CodeInstruction.blocks), out blocksObject) is false)
                            return false;
                        blocks = blocksObject as List<ExceptionBlock>;
                        return blocks.Any();
                    });
                    if (pairInstruction != null)
                    {
                        pairStart = opIndex + 1;
                        pairEnd = pairStart + newInstructions.Skip(opIndex + 1).ToList().IndexOf(pairInstruction) - 1;
                        var newBetweenInstructions = newInstructions.GetRange(pairStart, pairEnd - pairStart);
                        var remaining = originalBetweenInstructions.Except(newBetweenInstructions).ToList();
                        return remaining.Any() is false;
                    }
                }
            }

            if (isStartBlock is null && isEndBlock != null)
            {
                var pairInstruction = originalInstructions.GetRange(0, originalIndex).LastOrDefault(instr =>
                {
                    if (unassignedValues.TryGetValue(instr, out unassigned) is false)
                        return false;
                    if (unassigned.TryGetValue(nameof(CodeInstruction.blocks), out blocksObject) is false)
                        return false;
                    blocks = blocksObject as List<ExceptionBlock>;
                    return blocks.Any();
                });
                if (pairInstruction == null)
                    return true;
                var pairStart = originalInstructions.GetRange(0, originalIndex).LastIndexOf(pairInstruction);
                var pairEnd = originalIndex;
                var originalBetweenInstructions = originalInstructions
                    .GetRange(pairStart, pairEnd - pairStart)
                    .Intersect(newInstructions);

                pairInstruction = newInstructions.GetRange(0, opIndex).LastOrDefault(instr =>
                {
                    if (unassignedValues.TryGetValue(instr, out unassigned) is false)
                        return false;
                    if (unassigned.TryGetValue(nameof(CodeInstruction.blocks), out blocksObject) is false)
                        return false;
                    blocks = blocksObject as List<ExceptionBlock>;
                    return blocks.Any();
                });
                if (pairInstruction == null)
                    return true;
                pairStart = newInstructions.GetRange(0, opIndex).LastIndexOf(pairInstruction);
                pairEnd = opIndex;
                var newBetweenInstructions = newInstructions.GetRange(pairStart, pairEnd - pairStart);
                var remaining = originalBetweenInstructions.Except(newBetweenInstructions);
                return remaining.Any() is false;
            }

            return true;

            // unclear or unexpected case, ok by default
        }

        private static IEnumerable ConvertInstructionsAndUnassignedValues(Type type, IEnumerable enumerable,
            out Dictionary<object, Dictionary<string, object>> unassignedValues)
        {
            var enumerableAssembly = type.GetGenericTypeDefinition().Assembly;
            var genericListType = enumerableAssembly.GetType(typeof(List<>).FullName);
            var elementType = type.GetGenericArguments()[0];
            var genericListTypeWithElement = genericListType.MakeGenericType(elementType);
            var listType = enumerableAssembly.GetType(genericListTypeWithElement.FullName);
            var list = Activator.CreateInstance(listType);
            var listAdd = list.GetType().GetMethod("Add");
            unassignedValues = new Dictionary<object, Dictionary<string, object>>();
            foreach (var op in enumerable)
            {
                var elementTo = ConvertInstruction(elementType, op, out var unassigned);
                unassignedValues.Add(elementTo, unassigned);
                _ = listAdd.Invoke(list, new[] {elementTo});
                // cannot yield return 'elementTo' here because we have an out parameter in the method
            }

            return list as IEnumerable;
        }

        private static IEnumerable ConvertToOurInstructions(IEnumerable instructions, Type codeInstructionType,
            List<object> originalInstructions, Dictionary<object, Dictionary<string, object>> unassignedValues)
        {
            var newInstructions = instructions.Cast<object>().ToList();

            var index = -1;
            foreach (var op in newInstructions)
            {
                index++;
                var elementTo = AccessTools.MakeDeepCopy(op, codeInstructionType);
                if (unassignedValues.TryGetValue(op, out var fields))
                {
                    var addExceptionInfo = ShouldAddExceptionInfo(op, index, originalInstructions, newInstructions,
                        unassignedValues);

                    var trv = Traverse.Create(elementTo);
                    foreach (var field in fields.Where(field =>
                        addExceptionInfo || field.Key != nameof(CodeInstruction.blocks)))
                        _ = trv.Field(field.Key).SetValue(field.Value);
                }

                yield return elementTo;
            }
        }

        private static bool IsCodeInstructionsParameter(Type type)
        {
            return type.IsGenericType &&
                   type.GetGenericTypeDefinition().Name.StartsWith("IEnumerable", StringComparison.Ordinal);
        }

        private static IEnumerable ConvertToGeneralInstructions(MethodInfo transpiler, IEnumerable enumerable,
            out Dictionary<object, Dictionary<string, object>> unassignedValues)
        {
            var type = transpiler.GetParameters()
                .Select(p => p.ParameterType)
                .FirstOrDefault(t => IsCodeInstructionsParameter(t));
            if (type == null)
            {
                // Create new because we need likely to convert because this interop is for HarmonyX 2.0 to 2.1
                unassignedValues = new Dictionary<object, Dictionary<string, object>>();
                return enumerable;
            }
            if (type != typeof(IEnumerable<CodeInstruction>))
                return ConvertInstructionsAndUnassignedValues(type, enumerable, out unassignedValues);
            unassignedValues = null;
            return enumerable as IList<CodeInstruction> ??
                   (enumerable as IEnumerable<CodeInstruction> ?? enumerable.Cast<CodeInstruction>()).ToList();
        }

        private static List<object> GetTranspilerCallParameters(ILGenerator generator, MethodInfo transpiler,
            MethodBase method, IEnumerable instructions)
        {
            var parameter = new List<object>();
            transpiler.GetParameters().Select(param => param.ParameterType).Do(type =>
            {
                if (type.IsAssignableFrom(typeof(ILGenerator)))
                    parameter.Add(generator);
                else if (type.IsAssignableFrom(typeof(MethodBase)))
                    parameter.Add(method);
                else if (IsCodeInstructionsParameter(type))
                    parameter.Add(instructions);
            });
            return parameter;
        }
    }
}