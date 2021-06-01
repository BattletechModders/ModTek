# Features
  
|                         | Untyped Merges | Merges           | Replacements | Deletions        | MDDB Update |
|-------------------------|----------------|------------------|--------------|------------------|-------------|
| Custom Streaming Assets | x              | x                | x            | -                | -           |
| StreamingAssets         | x              | x                | x            | -                | x           |
| Content Packs           | x              | x                | x            | -                | x           |
| New Mod Content         | -              | x                | x            | - <sup>(2)</sup> | x           |
| Custom Resources        | -              | - <sup>(1)</sup> | x            | - <sup>(2)</sup> | -           |

<sup>(1)</sup>:
Merges are delayed until load is requested, in order to support merging DLC content.
Custom resources are loaded outside of modtek.

<sup>(2)</sup>:
Older ModTek supports this, newer does not anymore. Merges are delayed until DLC loads, which is

Resources:
- Custom Streaming Assets: DebugSettings and GameTip found in StreamingAssets but not BTRL.
  
- StreamingAssets: Vanilla content found in BTRL.
  
- Content Packs: DLC content provided by HBS. Content pack ids:
  shadowhawkdlc, flashpoint, urbanwarfare, heavymetal
  
- New Mod Content: Content that is not from HBS. but still a BattleTechResourceType.
  
- Custom Resources: Content that is not of type BattleTechResourceType,
  useful to provide completely new or not yet exposed data.
  Examples are SoundBank(Def) and Video.

Resource Modification Types:
- Untyped Merges: All merges reference an id and type,
  if the type of a resource can be auto-detected,
  since there is only one id, auto-merging can happen.
  All base game files that are mergable only have one type per id.
  
- Merges: All typed merges requires the type to be specified,
  mods could have used the same name for different resources.
  Only CSV, JSON and TXT can be merged.
  JSON merges are done with Newtonsoft JSON.NET.
  Txt and CSV merges simply append any text to the existing file.
  
- Replacements: Instead of merging, one can specify a type,
  id and filepath to overwrite a file completely.
  This is recommended for large mod packs to make sure there are not several
  merges interfering which each other differently based on load order.
  For smaller mods, you don't want to overwrite changes made by other mods,
  there is a good chance using merges instead of replacements is better in that case.
  
- Deletions: Not supported at all (anymore) since base types that would be in MDDB can't be removed anyway yet. 
  Bigger mod packs disable instead of removing content, so deletion doesn't make much sense to keep around.
  
- MDDB Update: MDDB is used by the game to index and then find certain types of data:
  ContractOverride, LanceDef, PilotDef, SimGameEventDef, MechDef, WeaponDef, TurretDef, VehicleDef, UpgradeDef.
