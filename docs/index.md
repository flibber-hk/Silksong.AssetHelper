# AssetHelper

AssetHelper is a library with utilities for loading game assets.

## Usage

Add the following line to your .csproj:
```
<PackageReference Include="Silksong.AssetHelper" Version="1.0.0" />
```
The most up to date version number can be retrieved from [Nuget](https://www.nuget.org/packages/Silksong.AssetHelper).

You will also need to add a dependency to your thunderstore.toml:
```
silksong_modding-AssetHelper = "1.0.0"
```
The version number does not matter hugely, but the most up to date number can be retrieved from
[Thunderstore](https://thunderstore.io/c/hollow-knight-silksong/p/silksong_modding/AssetHelper/).
If manually uploading, instead copy the dependency string from the Thunderstore link.

AssetHelper should be added as a BepInEx dependency by putting the following attribute
onto your plugin class, below the BepInAutoPlugin attribute.
```
[BepInDependency(org.silksong-modding.assethelper)]
```
