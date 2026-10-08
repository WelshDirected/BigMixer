# Mixing Station Mk3

A [MelonLoader](https://github.com/LavaGang/MelonLoader) mod for **Schedule I** that adds the **Mixing Station Mk3**, a mixing station that mixes **40 product with 40 ingredients** in a single batch.

## Features
- **40 × 40 batches.** The station has two product slots, two ingredient slots and two output slots. Each holds the normal stack of 20.
- **Same speed per batch.** A full batch of 40 takes about as long as a full Mk2 batch of 20.
- **Matches the game's look.** The Mk3 is built at runtime from the game's own Mk2 model, with red paint and a gold "MK3" badge. Its icon is rendered the same way as the game's own item icons.
- **Warehouse exclusive.** Only Oscar sells it, in person at the warehouse, for **$8,000**. It isn't in the phone delivery app.
- **Works with employees.** Chemists can load, start and collect from it. The start threshold goes up to 40.
- **Saves normally.** Both slots of each pair are saved, along with any mix in progress.

## Requirements
- Schedule I on the **Mono branch** ("alternate"). Tested on **0.4.6f13 Alternate**. The default IL2CPP branch is not supported.
- **MelonLoader 0.7.x**

## Installation
1. Download `MixingStationMk3-vX.Y.Z.zip` from [Releases](../../releases).
2. Copy `Mods/BigMixer.dll` into `...\steamapps\common\Schedule I\Mods\`.
3. Launch the game, then go and visit Oscar.

## Configuration
After the first launch, edit `UserData/MelonPreferences.cfg`:

```ini
[MixingStationMk3]
Price = 8000.0     # price at Oscar's warehouse
DevDump = false    # log the Mk2 model and mixing screen layout (for development)
```

## Building from source
You need the .NET 8 SDK (the build uses its Roslyn compiler) and a Mono install of Schedule I with MelonLoader. The build references the DLLs in your own game folder; nothing from the game is committed to this repo.

```powershell
./build.ps1                     # builds BigMixer.dll in this folder
./build.ps1 -Install            # builds it and copies it into the game's Mods folder
./build.ps1 -GameDir "D:\Games\Schedule I"   # use this if the game isn't in the default Steam path
```

## How it works
| File | What it does |
|---|---|
| `BigMixer.cs` | Mod entry point and settings. Registers the Mk3 each time the Main scene loads. |
| `Mk3Factory.cs` | Clones the Mk2 prefab and item definition into the Mk3, and registers it with the item registry and FishNet. |
| `Mk3Style.cs` | Repaints the clone and adds the "MK3" badge. |
| `Mk3Icon.cs` | Renders the icon with the game's `RuntimePreviewGenerator`. |
| `Mk3Slots.cs` | Adds the second slots. They slide into the first slot when it empties, so the game's single-slot code keeps working. Also handles mix quantity, splitting the output, and save/load. |
| `Mk3Ui.cs` | Adds the extra slots to the mixing station screen. |
| `OscarShop.cs` | Adds the in-person listing to Oscar's shop. |

Item stacks in Schedule I are hard-capped at their stack limit (20). That is why the Mk3 uses two slots per side instead of bigger slots.

## Notes
- **Uninstalling:** any placed Mk3 stations, and everything inside them, are lost the next time the save loads. Empty them first.
- **Co-op:** every player needs the mod installed.
- **Back up your saves** before trying any mod.

## Credits
Made by Ashton. The Mk3 is made from the game's own Mk2 model while the game runs. No game files or decompiled code are included in this repo or its releases.

Schedule I is developed by TVGS. This is an unofficial fan mod.
