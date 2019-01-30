namespace ModTekInjector
{
    /***
     * A container for the values passed by the user into the program via command line.
     * Interacts with OptionSet in the main injector class.
     */
    internal class ReceivedOptions
    {
        public enum Operation
        {
            Help,
            Detect,
            GameVersion,
            Version,
            Restore,
            Install,
        }

        public bool RequireKeyPress = true;
        public string RequiredGameVersion = string.Empty;
        public string RequiredGameVersionMismatchMessage = string.Empty;
        public string ManagedDir = "../BattleTech_Data/Managed";
        public string FactionsPath = string.Empty;
        public Operation PerformOperation = Operation.Install;
    }
}
