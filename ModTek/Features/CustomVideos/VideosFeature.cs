using System.IO;
using System.Linq;
using BattleTech;
using ModTek.Features.CustomResources;
using ModTek.Features.Manifest.BTRL;

namespace ModTek.Features.CustomVideos;

internal static class VideosFeature
{
    internal static VersionManifestEntry GetVideo(string videoName)
    {
        return BetterBTRL.Instance.AllEntriesOfType(InternalCustomResourceType.Video.ToString())
            .LastOrDefault(entry => entry.Id == videoName || entry.Id == Path.GetFileNameWithoutExtension(videoName));
    }
}