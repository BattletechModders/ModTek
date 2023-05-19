# Advanced JSON Merging

Using the standard merging mechanisms, one can't merge arrays or remove data.
Advanced JSON Merging adds several ways to surgically manipulate existing JSONs using [JSONPath](https://goessner.net/articles/JsonPath/).

To use Advanced JSON Merging, add a manifest to your `mod.json` and add an "AdvancedJSONMerge" type entry to it. All jsons found under Path will be assumed to be AdvancedJSONMerge instruction files.

Example Manifest with Advanced JSON Merging entry.

```JSON
{
    "Manifest": [
        {
            "Type": "AdvancedJSONMerge",
            "Path": "advanced"
        }
    ]
}
```

Example Advanced JSON Merging instructions json `advanced/blackknight_changes.json`

Removes all heat sinks from the mech and then adds in back two heat sinks in the center torso.

```JSON
{
    "TargetID": "mechdef_blackknight_BL-6-KNT",
    "Instructions": [
        {
            "JSONPath": "inventory[?(@.ComponentDefID == 'Gear_HeatSink_Generic_Standard')]",
            "Action": "Remove"
        },
        {
            "JSONPath": "inventory",
            "Action": "ArrayConcat",
            "Value": [
                {
                    "MountedLocation": "CenterTorso",
                    "ComponentDefID": "Gear_HeatSink_Generic_Standard",
                    "ComponentDefType": "Upgrade",
                    "DamageLevel": "Functional"
                },
                {
                    "MountedLocation": "CenterTorso",
                    "ComponentDefID": "Gear_HeatSink_Generic_Standard",
                    "ComponentDefType": "Upgrade",
                    "DamageLevel": "Functional"
                }
            ]
        }
    ]
}
```

## JSONPath Examples

[JSONPath](https://goessner.net/articles/JsonPath/) is a good documented standard for navigating jsons.
One can also find lots of solutions to problems on [stackoverflow](https://stackoverflow.com/questions/tagged/jsonpath).

## Action Examples

The sources of ModTek contain unit tests with some examples on how to use Actions.

`ArrayAdd` adds a given value to the end of the target array.

```JSON
{
    "JSONPath": "inventory",
    "Action": "ArrayAdd",
    "Value": {
        "MountedLocation": "CenterTorso",
        "ComponentDefID": "Gear_HeatSink_Generic_Standard",
        "ComponentDefType": "Upgrade",
        "DamageLevel": "Functional"
    }
}
```

`ArrayAddAfter` adds a given value after the target element in the array.

```JSON
{
    "JSONPath": "inventory[0]",
    "Action": "ArrayAddAfter",
    "Value": {
        "MountedLocation": "CenterTorso",
        "ComponentDefID": "Gear_HeatSink_Generic_Standard",
        "ComponentDefType": "Upgrade",
        "DamageLevel": "Functional"
    }
}
```

`ArrayAddBefore` adds a given value before the target element in the array.
`inventory[-1:]` references the last element of the inventory array.
Example adds a component to the second last position of the inventory.

```JSON
{
    "JSONPath": "inventory[-1:]",
    "Action": "ArrayAddBefore",
    "Value": {
        "MountedLocation": "CenterTorso",
        "ComponentDefID": "Gear_HeatSink_Generic_Standard",
        "ComponentDefType": "Upgrade",
        "DamageLevel": "Functional"
    }
}
```

`ArrayConcat` adds a given array to the end of the target array.
Allows to add multiple elements quickly without having to "ArrayAdd" them individually.

```JSON
{
    "JSONPath": "inventory",
    "Action": "ArrayConcat",
    "Value": [
        {
            "MountedLocation": "CenterTorso",
            "ComponentDefID": "Gear_HeatSink_Generic_Standard",
            "ComponentDefType": "Upgrade",
            "DamageLevel": "Functional"
        },
        {
            "MountedLocation": "CenterTorso",
            "ComponentDefID": "Gear_HeatSink_Generic_Standard",
            "ComponentDefType": "Upgrade",
            "DamageLevel": "Functional"
        }
    ]
}
```

`ObjectMerge` merges a given object with the target objects.
Example selects the head location and sets new armor values.

```JSON
{
    "JSONPath": "Locations[?(@.Location == 'Head')]",
    "Action": "ObjectMerge",
    "Value": {
        "CurrentArmor": 100,
        "AssignedArmor": 100
    }
}
```

`Remove` removes the target element(s).
Example removes all components from inventory that are heat sinks.

```JSON
{
    "JSONPath": "inventory[?(@.ComponentDefID == 'Gear_HeatSink_Generic_Standard')]",
    "Action": "Remove"
}
```

`Replace` replaces the target with a given value.
Example replaces the mech tags with a new list of tags.

```JSON
{
    "JSONPath": "MechTags.items",
    "Action": "Replace",
    "Value": [
        "unit_mech",
        "unit_heavy",
        "unit_role_brawler"
    ]
}
```
