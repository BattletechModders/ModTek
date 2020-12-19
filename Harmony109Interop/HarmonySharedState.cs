using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyXInterop;

namespace Harmony
{
	internal class PatchHandler
    {
        private MethodBase mb;
        private PatchInfoWrapper previousState = new PatchInfoWrapper
        {
            prefixes = new PatchMethod[0],
            postfixes = new PatchMethod[0],
            transpilers = new PatchMethod[0],
            finalizers = new PatchMethod[0]
        };
        
        public void Apply()
        {
            PatchMethod[] ToPatchMethod(Patch[] patches)
            {
                return patches.Select(p => new PatchMethod
                {
                    after = p.after,
                    before = p.before,
                    method = p.patch,
                    priority = p.priority,
                    owner = p.owner,
                }).ToArray();
            }

            var info = HarmonySharedState.GetPatchInfo(mb);
            var state = new PatchInfoWrapper
            {
                prefixes = ToPatchMethod(info.prefixes),
                postfixes = ToPatchMethod(info.postfixes),
                transpilers = ToPatchMethod(info.transpilers),
                finalizers = new PatchMethod[0]
            };
            
            var add = new PatchInfoWrapper { finalizers = new PatchMethod[0] };
            var remove = new PatchInfoWrapper { finalizers = new PatchMethod[0] };
            
            Diff(previousState.prefixes, state.prefixes, out add.prefixes, out remove.prefixes);
            Diff(previousState.postfixes, state.postfixes, out add.postfixes, out remove.postfixes);
            Diff(previousState.transpilers, state.transpilers, out add.transpilers, out remove.transpilers);

            previousState = state;
            
            HarmonyInterop.ApplyPatch(mb, add, remove);
        }
        
        static void Diff(PatchMethod[] last, PatchMethod[] curr, out PatchMethod[] add, out PatchMethod[] remove)
        {
	        add = curr.Except(last, PatchMethodComparer.Instance).ToArray();
	        remove = last.Except(curr, PatchMethodComparer.Instance).ToArray();
        }
        
        static Dictionary<MethodBase, PatchHandler> patchHandlers = new Dictionary<MethodBase, PatchHandler>();
        
        internal static PatchHandler Get(MethodBase method)
        {
	        lock (patchHandlers)
	        {
		        if (!patchHandlers.TryGetValue(method, out var handler))
			        patchHandlers[method] = handler = new PatchHandler {mb = method};
		        return handler;
	        }
        }
    }
	
	public static class HarmonySharedState
	{
		static Dictionary<MethodBase, PatchInfo> patchInfos = new Dictionary<MethodBase, PatchInfo>();

		internal static PatchInfo GetPatchInfo(MethodBase method)
		{
			lock (patchInfos)
			{
				if (!patchInfos.TryGetValue(method, out var info))
					patchInfos[method] = info = new PatchInfo();
				return info;
			}
		}

		internal static IEnumerable<MethodBase> GetPatchedMethods()
		{
			lock (patchInfos)
			{
				return patchInfos.Keys.ToList().AsEnumerable();
			}
		}
	}
}