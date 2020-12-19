# prefab-library
Prefab Library is a optimized tool that allows easy worldbuilding by stripping away all of the annoyances - no models (unless you want them), no meshes, no nothing! Just a searchbar, a tag filter, and a list of all results. Folders and tags can be whitelisted and blacklisted to ensure you only see what you want. This tool is extendable too - if you want certain scriptable objects to be drag-and-droppable (such as item assets) you can easily implement that. 

![Test Image 3](https://ardenfall.com/files/prefab-library-tool.gif)

## Settings
Settings are stored in resources - the file is called assetlibrary. It will automatically be created, but feel free to put it in whatever resource folder you wish.

```
Root Folders: What folders the tool will search for assets in. By default it is the "Assets/" folder, and thus will search in the entire project. 

Blacklist Folders: Folders to skip the scan. If there are certain folders inside of the root folders you wish to skip, put them here.

Blacklist Labels: Any assets with these labels will be hidden in the tool. Note the "BL" button on the tool will allow for overriding this.
```

## Labels
All labels in scanned assets will be displayed in the label dropdown. Selecting a label will filter the results. Selecting multiple labels will search for objects that have any of the selected labels.

The "BL" button will allow to toggle the blacklist label feature. 

## Custom Tools
Adding a new custom tool is easy. For scriptable objects, merely create a new class based on BaseScriptableObjectAssetLibraryTool, and fill in a few functions! You can also override the toolbar to add your own buttons and such, as well as modify the hoverbox to add your own info.

```
GetScriptableObjectType(): Returns the type of scriptable object you're handling

GenerateScriptableObjectThumbnail(index): Returns the thumbnail of the asset index. Note you can use GetAsset(index) to get the actual referenced asset.

EnableDragIntoScene(): Returns whether you wish to enable the drag-and-drop feature

CreateGhostPrefab(index): Used to create a "ghost" gameobject when dragging - this is a temporary visual that appears in the scene as the user drags the scriptable object. Note this needs to be an instantiated object.

OnPlaceInScene(index, position): Triggered when the user has "dropped" the object into the scene.  
```
You can also add your own non-scriptable object tools as well, of course. Just make a class based on AssetLibraryTool and do whatever you want!

## Future Plans
The biggest feature I need to add is the ability to drag and drop objects into the hierarchy, as opposed to the sceneview. I'd also like to allow multiple subtools to be active at once, since switching can be a pain. Adding support to make custom thumbnail generation modular would be great - to add your own thumbnail generator you need to modify a line in my code, which I do not like. Finally, making the searcher handle things asynchonously if possible would also be a big plus.
