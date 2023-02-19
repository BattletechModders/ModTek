using System.Collections.Generic;
using ModTek.Features.CustomResources;

namespace ModTek.Public;

public static class CustomResources
{
    public static IReadOnlyCollection<string> GetTypes()
    {
        return CustomResourcesFeature.GetTypes();
    }
}
