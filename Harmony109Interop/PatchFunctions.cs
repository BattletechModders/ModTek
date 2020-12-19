using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Harmony
{
	public static class PatchFunctions
	{
		public static void AddPrefix(PatchInfo patchInfo, string owner, HarmonyMethod info)
		{
			if (info == null || info.method == null) return;

			var priority = info.prioritiy == -1 ? Priority.Normal : info.prioritiy;
			var before = info.before ?? new string[0];
			var after = info.after ?? new string[0];

			patchInfo.AddPrefix(info.method, owner, priority, before, after);
		}

		public static void AddPostfix(PatchInfo patchInfo, string owner, HarmonyMethod info)
		{
			if (info == null || info.method == null) return;

			var priority = info.prioritiy == -1 ? Priority.Normal : info.prioritiy;
			var before = info.before ?? new string[0];
			var after = info.after ?? new string[0];

			patchInfo.AddPostfix(info.method, owner, priority, before, after);
		}

		public static void AddTranspiler(PatchInfo patchInfo, string owner, HarmonyMethod info)
		{
			if (info == null || info.method == null) return;

			var priority = info.prioritiy == -1 ? Priority.Normal : info.prioritiy;
			var before = info.before ?? new string[0];
			var after = info.after ?? new string[0];

			patchInfo.AddTranspiler(info.method, owner, priority, before, after);
		}

		public static List<MethodInfo> GetSortedPatchMethods(MethodBase original, Patch[] patches)
		{
			return patches
				.Where(p => p.patch != null)
				.OrderBy(p => p)
				.Select(p => p.GetMethod(original))
				.ToList();
		}
	}
}