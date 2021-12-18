using System.Collections.Generic;
using Newtonsoft.Json;

// ReSharper disable once CheckNamespace
namespace ModTek
{
    public class SoundBankDef
    {
        [JsonIgnore]
        public bool loaded { get; set; }

        public string name { get; set; }
        public string filename { get; set; }
        public List<uint> volumeRTPCIds { get; set; }
        public float volumeShift { get; set; }
        public SoundBankType type { get; set; }
        public Dictionary<string, uint> events { get; set; }

        public SoundBankDef()
        {
            events = new Dictionary<string, uint>();
            type = SoundBankType.Default;
            loaded = false;
            volumeRTPCIds = new List<uint>();
            volumeShift = 0f;
        }
    }
}
