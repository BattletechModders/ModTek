# Custom Tags and TagSets

ModTek 0.7.8 and above supports adding and update Tags and TagSets in the MetadataDatabase. HBS BT uses Tags for many different purposes, such as the pilot attribute descriptors, contract validation, and many more. You can add your own custom tags or update existing by adding the `CustomTag` and `CustomTagSet` type to your Manifest element: 
  
```json  
"Manifest": [ 
  	{ "Type": "CustomTag", "Path": "tags/" },
	{ "Type": "CustomTagSet", "Path": "tagSets/" }
],
```

> **Warning**
> There is currently no way to delete CustomTags or CustomTagSets. These are modified in the ModTek MDDB copy (located in `BATTLETECH/Mods/.modtek/Database/`) and should not alter the base MDDB located in the `BATTLETECH/Data` directory.

Each CustomTag needs to be defined in a .json file with the following structure:  
```json  
{
"Name" : "TAG_NAME_HERE",
"Important" : false,
"PlayerVisible" : true,
"FriendlyName" : "FRIENDLY_NAME",
"Description" : "DESCRIPTION TEXT"  
}
```

You can overwrite HBS defined tags with your own values by using the name tag-name. If multiple mods write to the same tag, the *last* mod to write the tag wins.

Each CustomTagSet needs to be defined in a .json file with the following structure:

```json  
{
	"ID" : "TAGSET_ID",
	"TypeID" : SEE_BELOW,
	"Tags" : [ "TAG_1" , "TAG_2", "TAG_3" ]  
}
```

As with CustomTags, you can update HBS defined TagSets by using the same ID for your  CustomTag. Any existing tags will be removed, and replaced with the tags you defined instead. You **must** include all tags you want in the updated TagSet in your CustomTag definition.

Note that the TypeID is an enumeration type, that unfortunately was never upgraded to a data-driven enum. Thus you are limited to the following enum types, as defined in the `Assembly-CSharp.dll`. You **must not** use a TypeID that's not defined below, or you will experience erratic behaviors. If the internal logic cannot cast your specified value to the defined enum it will likely fail without printing any error message.

| TagSetType | TypeID |
| ---------- | ------ |
| UNDEFINED | 1 |
| Map | 2 |
| Encounter | 3 |
| Contract | 4 |
| LanceDef | 5 |
| UnitDef | 6 |
| PilotDef | 7 |
| RequirementDefRequirement | 8 |
| RequirementDefExclusion | 9 |
| BiomeRequiredMood | 10 |
| Mood | 11 |
| EventDefRequired | 12 |
| EventDefExcluded | 13 |
| EventDefOptionRequired | 14 |
| EventDefOptionExcluded | 15 |
| EventDefAdded | 16 |
| EventDefRemoved | 17 |
| UnitDef_RequiredToSpawnCompany | 18 |
