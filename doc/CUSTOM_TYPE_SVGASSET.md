# Custom SVG Assets

Custom SVG assets can be added through the `SVGAsset` custom type. Any files at these paths will be added to the HBS DataManager and can be referenced through a loadrequest on the datamanager.
  
Simply define the path to your SVGs:
  
```json    
{ "Type": "SVGAsset", "Path": "icons/" },
```

then in your DLL mod read them from the DataManager:

```csharp    
  
# Load the file
DataManager dm = UnityGameInstance.BattleTechGame.DataManager;
LoadRequest loadRequest = dm.CreateLoadRequest();
loadRequest.AddLoadRequest<SVGAsset>(BattleTechResourceType.SVGAsset, "icon_foo", null);  
loadRequest.ProcessRequests();  
  
...  

# Read it
SVGAsset icon = DataManager.GetObjectOfType<SVGAsset>("icon_foo", BattleTechResourceType.SVGAsset);

```
