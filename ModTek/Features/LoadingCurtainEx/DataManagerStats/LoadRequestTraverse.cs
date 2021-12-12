using BattleTech.Data;
using Harmony;

namespace ModTek.Features.LoadingCurtainEx.DataManagerStats
{
    internal class LoadRequestTraverse
    {
        internal readonly LoadRequest instance;
        private readonly Traverse traverse;

        internal LoadRequestTraverse(LoadRequest instance)
        {
            this.instance = instance;
            traverse = Traverse.Create(instance);
        }

        internal int GetActiveRequestCount() => traverse.Method("GetActiveRequestCount").GetValue<int>();
        internal int GetPendingRequestCount() => traverse.Method("GetPendingRequestCount").GetValue<int>();
        internal int GetCompletedRequestCount() => traverse.Method("GetCompletedRequestCount").GetValue<int>();
    }
}
