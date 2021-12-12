using ModTek.Features.Manifest;

// ReSharper disable once CheckNamespace
// ReSharper disable once UnusedMember.Global
namespace ModTek
{
    // this API is experimental
    // this class is trying to get compatibility with CustomPrewarm
    // TODO remove functions not needed after CustomPrewarm compatibility is established
    public static class PreloaderAPI
    {
        public static bool IsPreloading => ModsManifestPreloader.HasPreloader;
        public static int PreloadChecksFinishedCounter => ModsManifestPreloader.finishedChecksAndPreloadsCounter;
        public static bool FirstPreloadFinished => PreloadChecksFinishedCounter > 0 && !IsPreloading;
    }
}
