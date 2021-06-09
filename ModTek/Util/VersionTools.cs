namespace ModTek.Util
{
    internal static class VersionTools
    {
        internal static string ShortVersion => GitVersionInformation.FullSemVer;
        internal static string LongVersion => GitVersionInformation.InformationalVersion + " (" + GitVersionInformation.CommitDate + ")";
    }
}
