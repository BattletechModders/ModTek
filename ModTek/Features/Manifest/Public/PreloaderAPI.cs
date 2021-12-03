using ModTek.Features.Manifest;

// ReSharper disable once CheckNamespace
// ReSharper disable once UnusedMember.Global
namespace ModTek
{
    public static class PreloaderAPI
    {
        public static bool IsPreloading => ModsManifestPreloader.isPreloading;
    }
}
