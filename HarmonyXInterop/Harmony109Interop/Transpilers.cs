﻿using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace Harmony
{
	public static class Transpilers
	{
		public static IEnumerable<CodeInstruction> MethodReplacer(this IEnumerable<CodeInstruction> instructions, MethodBase from, MethodBase to)
		{
			foreach (var instruction in instructions)
			{
#pragma warning disable CS0252, CS0253
                if (instruction.operand == from)
#pragma warning restore CS0252, CS0253
                    instruction.operand = to;
				yield return instruction;
			}
		}

		public static IEnumerable<CodeInstruction> DebugLogger(this IEnumerable<CodeInstruction> instructions, string text)
		{
			yield return new CodeInstruction(OpCodes.Ldstr, text);
			yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(FileLog), nameof(FileLog.Log)));
			foreach (var instruction in instructions) yield return instruction;
		}
	}
}