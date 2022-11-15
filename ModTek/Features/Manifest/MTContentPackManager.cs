using System.Collections.Generic;
using System.IO;
using BattleTech;
using BattleTech.Data;
using ModTek.Features.Manifest.BTRL;
using UnityEngine;

namespace ModTek.Features.Manifest;

internal class MTContentPackManager
{
    private Dictionary<string, AssetBundle> loadedBundles = new Dictionary<string, AssetBundle>();

    internal void LoadAllContentPacks()
    {
        var entries = BetterBTRL.Instance.AllEntriesOfResource(BattleTechResourceType.ContentPackDef);
        foreach (var entry in entries)
        {
            var def = new ContentPackDef();
            var json = File.ReadAllText(entry.FilePath);
            def.FromJSON(json);
            Load(def.Name);
        }
    }

    private void Load(string name)
    {
        if (loadedBundles.ContainsKey(name))
        {
            return;
        }
        var entry = BetterBTRL.Instance.EntryByID(name, BattleTechResourceType.AssetBundle);
        if (entry == null)
        {
            Log.Main.Info?.Log($"Can't find bundle {name} for loading");
            return;
        }
        var bundle = AssetBundle.LoadFromFile(entry.FilePath);
        loadedBundles.Add(name, bundle);
        var manifestCsv = bundle.LoadAsset<TextAsset>(name);
        var addendum = new VersionManifestAddendum(AddendumNameFromBundleName(name));
        using (var stringReader = new StringReader(manifestCsv.text))
        using (var csvReader = new CSVReader(stringReader))
            addendum.LoadFromCSV(csvReader);
        BetterBTRL.Instance.ApplyAddendum(addendum);
    }

    internal void UnloadAll()
    {
        foreach (var kv in loadedBundles)
        {
            var name = kv.Key;
            var bundle = kv.Value;
            BetterBTRL.Instance.RemoveAddendum(AddendumNameFromBundleName(name));
            bundle.Unload(true);
        }
        loadedBundles.Clear();
    }

    internal string GetText(string bundleName, string resourceId)
    {
        if (!loadedBundles.TryGetValue(bundleName, out var bundle))
        {
            Log.Main.Info?.Log($"Could not find bundle {bundleName}.");
            return null;
        }

        var asset = bundle.LoadAsset<TextAsset>(resourceId);
        if (asset == null)
        {
            Log.Main.Info?.Log($"Could not find resource {resourceId} in bundle {bundleName}.");
            return null;
        }
        return asset.text;
    }

    private string AddendumNameFromBundleName(string name)
    {
        return name + "Manifest";
    }
}