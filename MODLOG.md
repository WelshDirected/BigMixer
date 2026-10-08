# Mixing Station Mk3 journal

2026-10-08 — Goal: new "Mixing Station Mk3" (40 product + 40 ingredient per batch), game art style, sold only by Oscar (warehouse / DarkMarket).

Recon (reused PocketPlanner's MODDING_PLAN): Schedule I 0.4.6f13 Alternate (Mono), Unity 2022.3.62f2, MelonLoader 0.7.3 net35. Decompile at `Desktop/Work/Modding/Decompiled/Schedule 1`. FishNet decompile at `~/s1-fishnet/out`. Other mods in Mods/: BetterOrderHUD, Gta4InSchedule1, PocketPlanner, ShopKeeperDistribution, SkipTo8PM.

Facts:
- `MixingStation.MaxMixQuantity` (default 10; Mk2 prefab higher) and `MixTimePerItem` are public prefab fields. `GetMixQuantity = min(product, mixer, MaxMixQuantity)`.
- Slots cap at `ItemInstance.StackLimit` (from `BaseItemDefinition.StackLimit`, max 20 vanilla). Capacity checks: `ItemSlot.GetCapacityForItem` (virtual), `IsAtCapacity`, `AddItem` (CanStackWith checkQuantities), `ItemUIManager.EndDrag` (loops to StackLimit).
- Mix output: `OutputSlot.AddItem(def.GetDefaultInstance(qty))` into an empty slot → a 40 stack is fine.
- Placement/load: `BuildManager.CreateGridItem` instantiates `BuildableItemDefinition.BuiltItem` then `NetworkObject.Spawn`. Loaders look up the definition by ID in `Registry`.
- `Registry.RemoveRuntimeItems` runs on `onPreSceneChange` → re-add on every Main load.
- FishNet: `NetworkManager.SpawnablePrefabs.AddObject(nob, true)` → `InitializePrefabRange` assigns PrefabId.
- Oscar: `Oscar.ShopInterface`; `DeliveryApp.SetIsAvailable(Oscar.ShopInterface)` — listing `CanBeDelivered=false` keeps it in-person only. `ShopInterface.CreateListingUI`/`RefreshShownItems` are private.
- Icons: game's `IconGenerator` uses `RuntimePreviewGenerator.GenerateModelPreview` (global namespace, RuntimePreviewGenerator.Runtime.dll).

Route: MelonLoader C# + Harmony. Mk3 = runtime clone of the Mk2 prefab under an inactive DontDestroyOnLoad holder (scripts never run on the template), repainted + "MK3" TMP badge, MaxMixQuantity 40, per-item time scaled so a full batch takes as long as a full Mk2 batch. Definition = Instantiate(Mk2 def) with new ID `mixingstationmk3`. Icon rendered from a script-free mesh proxy. Slot patches raise capacity only on stations whose MaxMixQuantity > stack limit.

Model: Claude Design makes 2D designs, not Unity meshes, and fal MCP is disconnected (401). Kitbashing the game's own Mk2 meshes keeps the exact art style and ships no game files.

Build: `build.ps1` (Roslyn csc from dotnet SDK 8, langversion latest). Built OK.

Next: back up saves, install, launch, DevDump=true to read Mk2 hierarchy, tune badge position/paint, verify buy at Oscar → place → 40/40 mix → save/load.

2026-10-08 — User chose install-only, no save backup (no restore path exists). Price default set to $10 for testing; DevDump default true. Installed Mods/BigMixer.dll. Not yet run in game.

2026-10-08 — Play-test bug: dropping 20 then 20 lost 20. Cause: BaseItemInstance.ChangeQuantity refuses totals above StackLimit (20) (SetQuantity caps), so the old over-stack slot patch removed items from the source while the target refused them. Redesign (user's idea): no stack-limit changes; Mk3 gets ProductSlot2/MixerSlot2/OutputSlot2 (Mk3Station component added in MixingStationMk2.Awake postfix, identified by object name MixingStationMk3*). Server-side compaction slides slot2 into slot1 when slot1 empties, so vanilla single-slot code (GetProduct, chemist output collection, UI output checks) keeps working. GetMixQuantity sums both; ChangeQuantity prefix takes overflow from slot2; output split per StackLimit; save writes 2-entry ItemSets, loader postfix fills index 1. UI: cloned ItemSlotUI widgets placed under originals (positions unverified; DevDump logs the interface tree on first open).

2026-10-08 — User play-tested the two-slot build: 'Everything works well'. Still on test settings: Price $10, DevDump true.

2026-10-08 — Final settings: Price 8000, DevDump false (defaults and the user's MelonPreferences.cfg). Rebuilt and installed.
