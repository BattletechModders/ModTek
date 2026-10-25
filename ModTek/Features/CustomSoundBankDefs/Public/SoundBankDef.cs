using System.Collections.Generic;
using System.Runtime.InteropServices;
using Newtonsoft.Json;

// ReSharper disable once CheckNamespace
namespace ModTek;

public class SoundBankDef
{
    [JsonIgnore]
    public bool loaded { get; private set; }

    [JsonIgnore]
    private GCHandle _gcHandle;

    internal void Load(byte[] content, ref uint id, ref AKRESULT result)
    {
        _gcHandle = GCHandle.Alloc(content, GCHandleType.Pinned);
        try
        {
            result = AkSoundEngine.LoadBank(
                _gcHandle.AddrOfPinnedObject(),
                (uint)content.Length,
                out id
            );
            if (result == AKRESULT.AK_Success)
            {
                loaded = true;
            }
        }
        catch
        {
            result = AKRESULT.AK_Fail;
            Unload();
        }
    }

    internal void Unload()
    {
        loaded = false;
        if (_gcHandle.IsAllocated)
        {
            _gcHandle.Free();
            _gcHandle = default;
        }
    }

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