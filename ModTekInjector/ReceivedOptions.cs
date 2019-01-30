namespace ModTekInjector
{
    /***
     * A container for the values passed by the user into the program via command line.
     * Interacts with OptionSet in the main injector class.
     */
    internal class ReceivedOptions
    {
        public bool RequireKeyPress = true;
        public bool Detecting = false;
        public string RequiredGameVersion = string.Empty;
        public string RequiredGameVersionMismatchMessage = string.Empty;
        public string ManagedDir = "../BattleTech_Data/Managed";
        public bool GameVersion = false;
        public bool Helping = false;
        public bool Installing = false;
        public bool Restoring = false;
        public bool Updating = true;
        public bool Versioning = false;
        public string FactionsPath = string.Empty;
    }
}
