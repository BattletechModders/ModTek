using System.ComponentModel;
using System.IO;
using Newtonsoft.Json;

namespace ModTek.Mods
{
    public class ModState
    {
        [DefaultValue(true)]
        public bool Enabled { get; set; } = true;

        public static ModState CreateFromPath(string path)
        {
            var modState = JsonConvert.DeserializeObject<ModState>(File.ReadAllText(path));
            return modState;
        }

        public void SaveToPath(string path)
        {
            File.WriteAllText(path, JsonConvert.SerializeObject(this));
        }
    }
}
