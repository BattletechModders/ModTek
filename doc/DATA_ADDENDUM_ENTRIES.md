# Dynamic Enums
  dynamic enums handled outside manifest array. By DataAddendumEntries
  name - name of type. Supporting types BattleTech.FactionEnumeration, BattleTech.WeaponCategoryEnumeration, BattleTech.AmmoCategoryEnumeration, BattleTech.ContractTypeEnumeration
  path - path to file relative to mod root folder. Examples for content at BattleTech_Data\StreamingAssets\data\enums\ .

  example:
```
  "DataAddendumEntries":[
    {
      "name": "BattleTech.FactionEnumeration",
      "path": "Faction.json"
    },
    {
      "name": "BattleTech.WeaponCategoryEnumeration",
      "path": "WeaponCategory.json"
    }
  ]
```
