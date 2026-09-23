# CLAUDE.md

Context for Claude Code working in this repository. Auto-loaded at the start
of every session — keep it current as the project evolves; don't let it rot.

## Project

**ShinyHunt** (working title) — a first-person Unity game about shiny
hunting: capturing forest creatures and their rare color variants
(Normal/Uncommon/Rare/Legendary), inspired by Pokemon's shiny-hunting
mechanic. Folk-horror / found-footage / creepypasta tone. Protagonist Lucia
(13) is sent by her great-aunt Quiteria to catalog forest creatures, working
out of a hub cabin between expeditions.

Rendering: 3D scenes with 2D billboard sprite creatures (`Billboard.cs`),
lit via one shared cutout material rather than a material per creature (see
decision log).

## Where things live

> Adjust these paths to match the actual repo layout — this list was
> compiled from development history, not a live checkout. Fix it in your
> first real session here and it'll stay accurate after that.

- `Player/` — movement, health/"life", centralized freeze/unfreeze
- `Creatures/` (+ `Creatures/Animation/`) — species data, AI, flipbook animation
- `Inventory/`, `Tools/`, `Resources/` — the three separate inventories
- `Spawning/` — weighted creature spawners
- `CaptureMinigame/`, `CreatureTransform/`, `Dialogue/`, `Shop/`, `Stew/`,
  `RunSummary/`, `CritterDex/`, `UI/` — feature systems, each roughly
  self-contained
- `docs/systems.md` — full system-by-system map; read before touching an
  unfamiliar system
- `docs/decision_log.md` — why things are built the way they are; check
  before "fixing" something that looks off

## House conventions (violate these carefully, not by accident)

1. **ScriptableObjects are shared assets, never runtime state.** A
   `CreatureData`/`ToolData`/`ResourceData` asset is referenced by every
   instance of that type in the scene. Never write to a field on one at
   runtime (`data.rarity = X` is the exact bug this rule exists to prevent —
   see decision log). Per-instance runtime values (rolled rarity, current
   stack count, etc.) live on the `MonoBehaviour` or a plain runtime class
   instead.

2. **Persistent singleton pattern.** Every cross-scene manager
   (`InventoryManager`, `ToolInventoryManager`, `ResourceInventoryManager`,
   `PlayerHealth`, `PlayerStateManager`, `StewInventoryManager`,
   `ExpeditionStewManager`, `SceneTransitionManager`, `DialogueManager`)
   follows the same shape:

   ```csharp
   public static X Instance { get; private set; }
   void Awake() {
       if (Instance != null && Instance != this) { Destroy(gameObject); return; }
       Instance = this;
       DontDestroyOnLoad(gameObject);
   }
   ```

   These all live on one persistent `GameManagers` object, created once in
   the first-loaded scene. `UIManager` follows the same shape but lives on
   the persistent `Overlay` prefab root, which makes the whole HUD persistent
   — see "Overlay" below and the decision log before touching anything under it.

3. **`Start()` runs once, ever, on a persistent object.** Anything that
   needs to react per-scene-load on a `DontDestroyOnLoad` object must
   subscribe to `SceneManager.sceneLoaded`, not put logic in `Start()`.
   (`PlayerHealth` and `PlayerStateManager` both do this — copy their
   pattern for the next one.)

   **The Overlay is persistent** (only the first scene's instance survives;
   the copy in every other scene destroys itself, but its components still run
   `Awake` first). So anything under `Overlay`: subscribe in `OnEnable`/
   `OnDisable` (never once in `Start`), guard singleton/static setup in `Awake`
   against duplicates, hold no scene object references, and let `UIManager`
   show/hide expedition-only vs hub-only parts (`SceneNames.Hub`).

4. **Scene-scoped managers stay scene-scoped.** Not everything should be
   persistent — `CaptureMinigameController`, `CreatureTransformStationUI`,
   `BrewingStationUI`, shop panels, etc. hold direct references to scene
   objects and are deliberately NOT `DontDestroyOnLoad`.

5. **Popup UI pattern.** Every full-screen popup (shop, brewing, transform
   station, dialogue, capture minigame, run summary) follows: `Open()` /
   `Close()` toggling a `popupRoot`, calling `PlayerStateManager.Instance
.Freeze()` / `.Unfreeze()` — which centrally disables
   `FirstPersonController`, `Interactor`, `PlayerCapture`,
   `ToolEquipController` and toggles cursor lock. Don't reimplement this
   per-popup.

6. **Consumable actions signal completion via an event, not a return value
   read synchronously.** `ToolBehaviour.OnConsumed` (an `Action`) is fired
   by a tool subclass whenever its use is genuinely committed — which may be
   deep inside a coroutine, not the instant `UseTool()` returns. This exists
   because an earlier bool-return design caused a tool's own GameObject to
   be destroyed mid-coroutine by the inventory-triggered unequip sequence.
   Follow this pattern for any future action whose "did this count" resolves
   asynchronously.

7. **Angle convention for all wheel/dial UI**: degrees, clockwise, 0° = 12
   o'clock/top. Convert to a Unity Z rotation via negation
   (`Quaternion.Euler(0,0,-angle)`). Used by the capture minigame wheel and
   the expedition time dial — keep any new radial UI consistent with this.

8. **Grid UI rebuilds fully on refresh** rather than diffing (creature/tool/
   resource inventories, CritterDex, shop, stew inventory). Simpler and
   correct; only worth optimizing if profiling actually says so.

9. **Drag-and-drop UI** uses a shared "ghost" `Image` (a static field on the
   draggable entry class, set once by the owning popup's `Awake()`), plus
   `IBeginDrag`/`IDrag`/`IEndDrag` on the source and `IDropHandler` on BOTH
   the specific drop target and a full-panel fallback zone (so dropping into
   empty space still works).

10. **Balance numbers live in config `ScriptableObject`s**, not hardcoded
    constants — `StewCalculationConfig` is the biggest example. Tune there,
    not in code.

## Known simplifications / things to revisit

- **Ingredient Power** now rolls its double-resource chance at _capture_
  time (`CreatureAI.TryCapture`), not transform time — the flag is tracked as
  a per-(species,rarity) sub-count (`InventoryManager.sparkleCounts`), not
  true per-instance identity (captured creatures within a stack still aren't
  individually distinguishable — flagged units are just consumed first).
  Shown as a sparkle badge in both the regular inventory and the transform
  station. See decision log.
- **Stew "time cap"** is one global constant
  (`StewCalculationConfig.maxTimeSeconds`), not a per-bowl stat — revisit if
  bowl tiers get added.
- **Multi-hub bowl capacity** isn't built — `StewInventoryManager` has one
  capacity pool. If multiple hub scenes get added, `baseCapacity` needs to
  become keyed by scene.
- **Money system API** (`MoneyManager.GetCurrentMoney()` / `AddMoney(int)` /
  `TrySpendMoney(int)`) — confirmed working as of the shop/run-summary
  integration.
- Several systems carry an explicit code-comment flag for a known gap —
  these are intentional, left for later, not oversights.

## For Claude Code specifically

- Read `docs/systems.md` before making changes to a system you haven't
  touched yet — it has the cross-references (which managers call which).
- Check `docs/decision_log.md` before "fixing" something that looks like a
  bug — several things look odd on purpose (freeze/unfreeze ordering in
  dialogue choices, the Ingredient Power timing, etc.).
- When adding a new persistent manager, follow convention #2 exactly — it's
  what every other manager does and what other systems expect to find.
- This file and its two companions were reconstructed from a long chat
  thread, not generated from a live read of the repo. Treat the very first
  session here as a reconciliation pass: read the actual current files,
  correct anything in these docs that's drifted, and note the correction in
  `docs/decision_log.md` if the drift was itself informative.
