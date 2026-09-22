# Systems Map

What exists and how it connects. Not a substitute for reading the actual
code — this is the "why would I touch this file" index. See `CLAUDE.md` for
the conventions these all share, and `decision_log.md` for the reasoning
behind the less obvious choices called out here.

## Player Core

| File                       | Role                                                                                                                                                                                                                                                                                                                                  |
| -------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `FirstPersonController.cs` | WASD + mouselook movement, camera bob, hand bob (independent params from camera bob)                                                                                                                                                                                                                                                  |
| `PlayerCapture.cs`         | Left-hand "hit with stick" swing (stuns). Aims via `CreatureTargeting`, exposes `HitRange`/`HitRadius` so the doll knows what a click's stick swing can reach. Disabled on the hub's player. No longer touches run tracking                                                                                                                                                                                                                                                          |
| `PlayerHealth.cs`          | Persistent. The expedition "life"/time-remaining value. Inert while the active scene is the Hub. Exposes `AddTemporaryMaxHealth` (cleared on hub return — "for one run" effects), `AddPermanentMaxHealth` (forever), `AddBankedChronoBonus`/`ConsumeBankedChronoBonus` (survives hub return, consumed once by the next selected stew) |
| `PlayerStateManager.cs`    | Persistent. The single `Freeze()`/`Unfreeze()` entry point used by every popup. Re-finds player components on every scene load, since the player object itself isn't persistent                                                                                                                                                       |
| `UIManager.cs` (`UI/`)     | Persistent, on the root of the persistent `Overlay` prefab (HUD, tablet, run summary, dialogue panel). Shows/hides scene-dependent HUD parts on scene load (expedition-only vs hub-only) and owns the tablet (`I`/Esc) — see decision log |
| `Interactor.cs`            | Raycast-based `IInteractable` trigger (pre-existing, untouched throughout this whole build)                                                                                                                                                                                                                                           |

## Creatures

| File                                                                                                         | Role                                                                                                                                                                                                                                                                              |
| ------------------------------------------------------------------------------------------------------------ | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `CreatureData.cs`                                                                                            | ScriptableObject. Per-species config: visuals per rarity, movement/detection/flee tuning, resources per rarity, `family` (`IngredientFamily`), `scentPreference[5]`                                                                                                               |
| `CreatureAI.cs`                                                                                              | State machine (Idle/Wander/Flee/Stunned/Captured). Rolls rarity ONCE per instance in `Awake`, never writes back to the shared `CreatureData`. Implements `ICapturable`. Rarity roll and flee behavior are live-modified by `ExpeditionStewManager` (Shiny Power / Soothing Power) |
| `ICapturable.cs`                                                                                             | `OnHit()`, `IsStunned`, `TryCapture(float chance)` — chance is computed externally (by the capture minigame), never rolled internally                                                                                                                                             |
| `CreatureAnimState.cs`, `CreatureAnimationClip.cs`, `CreatureVariantVisuals.cs`, `CreatureSpriteAnimator.cs` | Frame-by-frame flipbook animation per rarity variant, fully decoupled from `CreatureAI` (which only ever calls `Play(state)`)                                                                                                                                                     |
| `CreatureTargeting.cs`, `CaptureTargetIndicator.cs` | Shared aim-cone targeting used by the stick and the doll (works from `CreatureAI.Active`, not physics), and the red/yellow/green ring drawn around the aimed-at creature while the doll is held. See decision log |
| `Billboard.cs`                                                                                               | Camera-facing rotation only. Lighting/fog is a material/shader concern, not this script                                                                                                                                                                                           |

## Inventories (three separate systems, same conventions)

| File                                                                                      | Role                                                                                                                                                                                            |
| ----------------------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `InventoryManager.cs`                                                                     | Captured creatures, keyed by (species, rarity). Also tracks per-run captures and first-ever-species-seen separately from the persistent totals — needed by `RunSummaryUI`. Run tracking resets itself on `sceneLoaded` for any non-hub scene                       |
| `ResourceData.cs` / `ResourceInventoryManager.cs`                                         | Crafting ingredients extracted from creatures. Dictionary + a separate ordered-key list backing a "Sort" button (see decision log for why a dictionary needs a side-list to be sortable at all) |
| `ToolData.cs` / `ToolBehaviour.cs` / `ToolInventoryManager.cs` / `ToolEquipController.cs` | 10-slot equippable hotbar. `ToolBehaviour.OnConsumed` event drives stack depletion — see CLAUDE.md convention #6                                                                                |

## Spawning

| File         | Role                                                                                                                                                                                                                           |
| ------------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| `Spawner.cs` | Weighted species list per spawn point, population cap with respawn delay. Weight is biased by (a) scent match between `CreatureData.scentPreference` and the active stew's scent profile, (b) Encounter Power's boosted family |

## Capture Minigame

| File                           | Role                                                                                                                                                                                                                                 |
| ------------------------------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| `CaptureHitAreaUI.cs`          | One arc on the wheel — drawn via `Image.FillMethod.Radial360`, not a custom mesh                                                                                                                                                     |
| `CaptureMinigameController.cs` | Spins the needle, resolves hits, computes final chance = hits/total + Capture Power's bonus. `ForceEndMinigame()` lets `RunSummaryUI` cleanly resolve an in-progress capture when the expedition's overall time runs out mid-attempt |

## Creature -> Resource Transform Station

| File                                                                                          | Role                                                                                                                                                                                                                                                                                               |
| --------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `CreatureTransformStationUI.cs` + drag UI (`CreatureTransformEntryUI`, `TransformDropZoneUI`) | Freezes the player via `PlayerStateManager` in `Open()`/`Close()`. Two-phase in design: `BeginTransform()` snapshots the selection, `CompleteTransform()` consumes creatures and grants resources — also where Ingredient Power's double-yield roll happens, per unit. NOTE: the animation hook is currently bypassed (`BeginTransform` calls `CompleteTransform` directly and the popup is closed by a separate `Close` button wired in the scene) |

## CritterDex (bestiary)

| File                                                                                                            | Role                                                                                                                   |
| --------------------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------- |
| `CritterDexRegistry.cs`                                                                                         | Ordered list of every species — defines dex numbers and what shows as a locked silhouette (a species not yet captured) |
| Grid/detail/entry UI (`CritterDexGridUI`, `CritterDexDetailUI`, `CritterDexEntryUI`, `CritterDexRarityBadgeUI`) | Locked = dark-tinted icon of the same sprite, not a separate silhouette asset                                          |

## Dialogue

| File                                      | Role                                                                                                                                                                                                     |
| ----------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `DialogueData.cs` / `DialogueNodeData.cs` | ScriptableObject node graph: lines, optional choices, `defaultNextNode` for auto-continue when there's no decision                                                                                       |
| `DialogueManager.cs`                      | Persistent runner. Typewriter text; choice buttons fire an `actionId` string via `OnChoiceAction` rather than embedding `UnityEvent`s in the data asset — keeps dialogue data reusable/scene-independent |
| `NPCDialogueTrigger.cs`                   | `IInteractable` glue for NPCs                                                                                                                                                                            |
| `DialogueActionRouter.cs`                 | Example/template for dispatching `actionId` to real functionality (opening the transform station, etc.) — scene-specific, not persistent                                                                 |

## Shop

| File                                                                  | Role                                                                                                   |
| --------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------ |
| `Shop.cs`                                                             | Pre-existing open/close/freeze, untouched throughout                                                   |
| `ShopCatalogUI.cs` / `ShopItemButtonUI.cs` / `ShopPurchasePanelUI.cs` | Grid + quantity-purchase sub-panel. Automatically refunds if the inventory can't fit the full purchase |
| `HoldButtonUI.cs`                                                     | Hold-to-repeat with acceleration, used by the purchase panel's quantity +/- buttons                    |
| `ShopMoneyDisplayUI.cs`                                               | Deliberately does NOT subscribe to `MoneyManager.OnMoneyChanged` for live updates — see decision log   |

## Expedition Lifecycle

| File                         | Role                                                                                                                                                                  |
| ---------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `ExpeditionTimeIndicator.cs` | Top-right clock-dial HUD for time remaining (a 0-1 ratio from `PlayerHealth`). Shown on expeditions, hidden in the hub by `UIManager`. On depletion it triggers `RunSummaryUI`, falling back to `SceneTransitionManager` straight to the hub if there is no summary |
| `RunSummaryUI.cs`            | Two-page end-of-run popup (creatures caught this run, stats + reward), then a money count-up animation before handing off to `SceneTransitionManager` for the fade back to the hub (stays frozen through the transition — see decision log). Part of the persistent Overlay |
| `SceneTransitionManager.cs`  | Persistent fade-to-black -> `SceneManager.LoadScene` -> fade-in. Persistent because the fade has to survive the very scene unload it triggers. `TransitionToScene` returns false if already transitioning / scene unloadable, and freezes the player for the whole transition |

## Stew System

| File                                                                                                                                                  | Role                                                                                                                                                                                                                            |
| ----------------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `IngredientFamily.cs` / `StewModifierType.cs`                                                                                                         | Shared enums                                                                                                                                                                                                                    |
| `StewCalculationConfig.cs`                                                                                                                            | Every tunable number for stew math — the single source of truth for balance                                                                                                                                                     |
| `StewCalculator.cs`                                                                                                                                   | Pure functions: ingredients -> time/scents/modifier. No Unity lifecycle involved, easy to reason about or test in isolation                                                                                                     |
| `StewInstance.cs`                                                                                                                                     | Runtime result of a brew — not a ScriptableObject, since each stew is a unique result, not an authored asset                                                                                                                    |
| `StewInventoryManager.cs`                                                                                                                             | Persistent. The "bowls."                                                                                                                                                                                                        |
| `ExpeditionStewManager.cs`                                                                                                                            | Persistent. THE central query point — every other system (`CreatureAI`, `Spawner`, `CaptureMinigameController`, `CreatureTransformStationUI`) asks this "what's my multiplier"; none of them know about `StewInstance` directly |
| `BrewingStationUI.cs` + supporting UI (`BrewIngredientEntryUI`, `BrewCauldronSlotUI`, `StewResultPopupUI`, `StewInventoryPanelUI`, `StewBowlEntryUI`) | The cauldron popup: 8 slots (4 unlocked by default), drag ingredients in, Brew -> result popup -> Accept/Dump                                                                                                                   |
| `ExpeditionStewSelectionUI.cs` + `StewCarouselEntryUI.cs`                                                                                             | Exit-door carousel: the selected bowl sits in the middle, Next/Previous slide a runtime-built track of bowls (not looped); confirm consumes the bowl and starts the scene transition. The always-available default stew (`ExpeditionStewManager.GetDefaultStew()`, flagged `isDefault`) is appended last and is never consumed                                                                                                                             |

## Cross-cutting dependency notes

- `ExpeditionStewManager` is read by: `CreatureAI` (Shiny/Soothing), `Spawner`
  (Encounter + scent bias), `CaptureMinigameController` (Capture),
  `CreatureTransformStationUI` (Ingredient). Chrono Power is the exception —
  it only ever touches `PlayerHealth`'s banked bonus; nothing reads it from
  `ExpeditionStewManager` directly.
- `PlayerStateManager` is called by every popup's `Open()`/`Close()`. If you
  add a new popup, use it — don't hand-roll freeze logic again.
- `SceneTransitionManager` is called by `RunSummaryUI` (end of run, via the
  summary), `ExpeditionTimeIndicator` (fallback when there is no summary) and
  `ExpeditionStewSelectionUI` (start of run). Route any other scene changes
  through it too, for the fade and the player freeze. Scene names the code
  branches on live in `SceneNames`.
