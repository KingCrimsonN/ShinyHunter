# Decision Log

Reasoning behind non-obvious choices, so they don't get "fixed" back into a
bug later. Append-only going forward — add an entry whenever a "wait, why is
this weird" moment gets resolved during review or a new session.

## Never mutate a shared ScriptableObject asset at runtime

Early `CreatureAI` rolled a creature's rarity by writing `data.rarity = X`
directly onto the `CreatureData` asset. Since every instance of a species
shares the same asset, two creatures of the same species active at once
would stomp each other's rolled rarity, and the asset would stay dirtied in
the Editor. Fixed by keeping rolled rarity as a private field on `CreatureAI`
itself. This is now the standing rule for ALL runtime-varying data derived
from a shared asset — see `CLAUDE.md` convention #1.

## Capture chance moved out of CreatureAI and into an explicit parameter

Originally `CreatureAI.TryCapture()` rolled against its own
`CreatureData.baseCaptureChance`. Once the capture minigame (wheel) was
built, the chance needed to come from the player's performance (hits/total)
instead, so `TryCapture` now takes an explicit `float captureChance`
computed by whoever is resolving the attempt. `baseCaptureChance` is left on
`CreatureData` but unused, in case it gets folded back in as a per-species
multiplier later.

## Tool consumption signaled via an event, not a UseTool() return value

First attempt: `UseTool()` returned `bool`, and `ToolEquipController`
removed one from the inventory stack the instant it returned `true`. This
broke the VoodooDoll tool, whose actual "commitment" (throwing, starting the
capture minigame) happens asynchronously inside a coroutine — the inventory
removal triggered an immediate unequip/destroy sequence that killed the
doll's GameObject mid-coroutine, producing a null reference. Fixed by adding
`ToolBehaviour.OnConsumed` (an event), raised by the tool itself whenever
it's actually safe — synchronously for simple tools, or as the last line of
a coroutine for tools like the doll.

## Scene transitions need a persistent fade

`ExpeditionTimeIndicator` originally owned its own blackout `CanvasGroup`
and called `SceneManager.LoadScene` from inside its own fade coroutine. Once
the hub became an actual separate Scene (not just a teleport within one
scene), this broke: loading a new scene destroys the current scene's
GameObjects, killing the coroutine (and the CanvasGroup) before the
fade-back-in half could run. Fixed by moving the fade into a dedicated
persistent `SceneTransitionManager` that survives the load.

## Dialogue choices dispatch by string ID, not embedded UnityEvents

A dialogue choice needing to trigger real functionality (open the transform
station, give money) could have used a `UnityEvent` serialized directly on
the `DialogueChoice` data. Rejected: `DialogueNodeData` is meant to be a
reusable data asset, and a `UnityEvent` pointing at a specific scene object
only works for that one scene/session. Instead, choices carry a plain
`actionId` string; `DialogueManager` fires it as an event, and a
scene-specific `DialogueActionRouter` (or equivalent) does the actual
dispatch via a switch statement. Keeps dialogue data fully portable.

## Freeze-then-act ordering in DialogueManager.SelectChoice

A choice that both ends the dialogue AND triggers an action that itself
freezes the player (e.g. opening another popup) needs the dialogue's own
`EndDialogue()` (which unfreezes) to run BEFORE the action fires (which may
re-freeze) — not after. Otherwise the dialogue's unfreeze would run last and
incorrectly override the action's freeze. This ordering is why
`SelectChoice` resolves the node transition first, then fires
`OnChoiceAction` second.

## Ingredient Power resolves at transform time, not capture time

The stew modifier "chance of double resources from a creature" naturally
reads as a per-capture roll. But `InventoryManager` only tracks counts per
(species, rarity), not individual creature instances, so there's no way to
tag "this specific captured rabbit is double-yield." Rolled instead at the
point of turning creatures into resources
(`CreatureTransformStationUI.CompleteTransform`), once per unit being
transformed. Same player-facing outcome, different resolution point —
documented as a deliberate simplification, not an oversight.

## Chrono Power is banked, not applied to the current run

Its stated effect is "extra time for the NEXT expedition." Since
`PlayerHealth`'s temporary max-health modifier is cleared automatically on
every hub arrival (deliberately, for normal "one run" effects), a bonus
meant for the _next_ stew can't live there — it would be wiped before the
next stew gets picked. It lives in a separate `bankedChronoBonus` field on
`PlayerHealth` that survives hub arrival and is only consumed once, by
`ExpeditionStewManager.SetActiveStew`.

## ShopMoneyDisplayUI doesn't subscribe to MoneyManager.OnMoneyChanged

Doing so seems like the obvious way to keep the display live, but by the
time that event fires, `MoneyManager`'s total has already changed — there'd
be no "before" value left to animate the count-down from. Since nothing else
can spend money while the shop is open (player is frozen), every update this
display needs goes through an explicit `AnimateFrom(previousAmount)` call
made right before the purchase's money deduction, instead.

## Billboard sprites use a shared Lit+cutout material, not per-creature materials

Sprites needed to receive lighting and fog, which the default unlit
`SpriteRenderer` material doesn't support. Rather than authoring a material
per creature (impractical at scale), a single shader is built with its
texture property's Reference explicitly named `_MainTex` —
`SpriteRenderer` automatically feeds each instance's own sprite texture into
any material with that property name, so ONE shared material serves every
creature regardless of species/rarity/frame. No C# changes were needed for
this — purely a shader/material asset change.

## The Overlay is persistent; it shows/hides its own parts per scene

`UIManager` sits on the root of the `Overlay` prefab and calls
`DontDestroyOnLoad`, which makes the WHOLE Overlay (time/health dial, tool
hotbar, tablet, run summary, dialogue panel...) persistent. Every scene still
contains its own Overlay instance, but only the first one survives — later
ones destroy themselves in `UIManager.Awake`. Decision: keep it that way
(one shared HUD) rather than making each scene's Overlay independent, and
have `UIManager` show/hide scene-dependent parts on every `sceneLoaded`
(expedition-only: `ExpeditionTimeIndicator`, `ToolHotbarUI`; extra roots via
its inspector arrays). Consequences that look odd on purpose:

- Everything under the Overlay subscribes in `OnEnable`/`OnDisable` (with a
  `Start` fallback for the very first scene), never once in `Start`, because
  `Start` only ever runs once and objects get hidden/re-shown.
- A duplicate Overlay's components still run `Awake` before it is destroyed,
  so singleton/static setup there is guarded (`RunSummaryUI.Awake`,
  `ToolInventoryPopupUI.Awake`'s `DragIcon`) instead of overwriting the
  survivor. `ApplyButtonSounds` only hooks buttons from the surviving instance.
- Scene code must reach the HUD through `UIManager.Instance`, never a cached
  `FindFirstObjectByType<UIManager>()` (can return the doomed duplicate).
- `UIManager` holds no player references; tablet freezing goes through
  `PlayerStateManager` like every other popup.
- Per-scene overrides on the Overlay instance only take effect if that
  scene loaded first. Apply layout changes to the prefab itself.

## A capture attempt suspends the creature's stun timer

The capture wheel used to hand the creature a 5 s stun (`StartCapture(5)`)
while the wheel itself runs 5 s plus a 0.5 s end-of-minigame delay. Any
attempt that ran the timer out (or finished its last hit after ~4.5 s) reached
`TryCapture` after the creature had already recovered, and the result was
silently discarded. `StartCapture()` now just flags `captureInProgress`;
`TickStunned` doesn't count down while it's set, and any transition out of
`Stunned` (a failed capture sends it fleeing) clears it. The creature stays
stunned exactly until the attempt is resolved.

## ForceEndMinigame leaves the player frozen

`RunSummaryUI` force-ends an in-progress capture and immediately opens its
own popup. `EndMinigame(forceEnd: true)` therefore does NOT unfreeze (it used
to unfreeze and the caller re-froze, which flickered the cursor), and
`ForceEndMinigame` also cancels a normal end that was already pending in the
0.5 s delay window — otherwise that pending end would fire later and unfreeze
the player behind the summary popup. `cooldown`/`ending` are reset on begin
and end, since `StopAllCoroutines` can kill `CoolDown()` mid-flight.

## SceneTransitionManager freezes the player for the whole transition

`TransitionToScene` now returns `bool` (false = a transition is already
running, or the scene isn't loadable) and freezes the player from the start of
the fade until the new scene has loaded. Callers must NOT unfreeze before
transitioning — `ExpeditionStewSelectionUI.Confirm` hides its popup but stays
frozen, and `RunSummaryUI` likewise. `PlayerStateManager.OnSceneLoaded`
re-applies the frozen state to the new scene's player, so there is no window
where the player can move (or reuse the door, consuming a second stew and
stacking `AddTemporaryMaxHealth`). `Confirm` starts the transition BEFORE
consuming the stew, so a failed transition doesn't lose it.
`ExpeditionTimeIndicator` no longer has its own blackout/`LoadScene`; it
previously reintroduced the destroyed-coroutine bug the persistent fade fixed.

## ToolInventoryManager.SortSlots copies values, not slot references

`ToolSlot` is a class and the sort's rewrite loop overwrote the same slot
objects it was still reading from, duplicating/deleting items whenever a
sorted item moved to a lower index. It now copies `(data, count)` out first.

## Per-run tracking resets when an expedition scene loads (InventoryManager)

`InventoryManager.ResetRunTracking()` used to be called from
`PlayerCapture.Start()`, only when the scene was `"Hub"`. But `PlayerCapture`
is disabled on the hub's player (`m_Enabled: 0`), so `Start` never ran there,
and expedition scenes skipped it because of the `"Hub"` check - the tracking
was never reset and every run summary included all earlier runs' creatures
(and their reward). It now lives in `InventoryManager` itself, on
`sceneLoaded` for any non-hub scene, so it can't be skipped by a disabled or
missing player component.

## Creature targeting is aim-cone based and shared (CreatureTargeting)

The doll used a thin `Physics.Raycast` and the stick a `SphereCast`, both
against a `creatureLayer` mask that is set to Everything (creatures are on
Default). Physics returns the FIRST collider of any kind - terrain, trees,
grass, props - and "not a creature" was treated as a miss, so looking straight
at a creature could fail. The doll (range 3-4) and stick (2.5) also disagreed
about range, and one click fires BOTH (a click swings the stick, which stuns,
and throws the doll, which captures ~0.4 s later if the creature is stunned).
`CreatureTargeting.TryFind` now works from the creatures themselves
(`CreatureAI.Active`) and an aim cone: closest to the crosshair wins, range is
measured to the nearest point of the creature's bounds, and other colliders
can't interfere. Both tools use it with the same aim forgiveness. It has no
line-of-sight check on purpose (a check against an Everything mask would bring
the original false blocking back) - add one with a dedicated occluder mask if
needed. `CaptureTargetIndicator` (a code-built ring, created by the doll while
held) shows the result: red = too far, yellow = in reach but not stunned (the
click's stick swing will stun it - only valid within the stick's reach too),
green = ready. `ToolBehaviour.Update` was renamed `OnHeldUpdate` (a virtual
named `Update` ran twice per frame). `CaptureMinigameController.BeginCapture`
returns whether it started, and the doll is only consumed if it did.

## Stew carousel slides a track, it doesn't rebuild the layout

The carousel panel (`Horizontal Carousel`) has a visible background image and
its own `HorizontalLayoutGroup`, so moving the panel or its entries directly
would either move the background or fight the layout group. Bowls are
spawned into a runtime-created `CarouselTrack` child (which copies the
panel's spacing / top+bottom padding / alignment so the look still follows
the panel's inspector settings, and is ignored by the panel's own layout
group). The selected bowl's measured centre is what the track eases toward
(`SmoothDamp`), so the selected bowl is always in the middle and Next/Previous
slide the whole row. Not looped: the buttons disable at either end. Entries
scale/highlight by their continuous distance from the centre (`SetFocus`).
A `StewCarouselEntryUI` placed under the panel at design time is treated as
a layout preview and hidden at runtime (it used to sit beside the real ones).

## The default stew (Auntie's Stew) is not a bowl

The exit carousel must never be empty, so a built-in stew is always offered.
It is deliberately NOT stored in `StewInventoryManager`: it takes no bowl
capacity (so it can't make "all bowls full" block brewing), it doesn't appear
in the bowl inventory panel, and it is never consumed. `StewInstance.isDefault`
marks it; `ExpeditionStewSelectionUI.Confirm` skips `RemoveStew` for it and
otherwise runs the exact same `SetActiveStew` path (so time and any banked
Chrono bonus apply normally). It is appended LAST in the carousel, so the first
selection is a real bowl when the player has any. Its numbers (name, time,
scents, icon) live on `StewCalculationConfig`, per the balance-in-config
convention, and `ExpeditionStewManager.GetDefaultStew()` builds a fresh runtime
`StewInstance` from them on each call - the shared config asset is never written
to. It has no modifier by design; the point is a safe baseline, not a build.

## Scrollbar handles have a fixed size (FixedSizeScrollbarHandle)

A `ScrollRect` writes `Scrollbar.size` (= viewport / content) whenever content or
viewport changes, and the `Scrollbar` stretches the handle's anchors to that
fraction, so the handle length varies with the amount of content (and fills the
whole track when nothing scrolls). Fixed handle art needs a constant length that
still travels the full track. `Scrollbar.size` isn't virtual and the anchors are
driven by Unity, so rather than fight that, `FixedSizeScrollbarHandle` puts
`size` back to a fixed value AFTER the ScrollRect has written it
(`DefaultExecutionOrder(1000)`, applied in `LateUpdate`, so the wrong size is
never rendered). Scroll position (`value`) stays ScrollRect-driven, and Unity's
own drag handling reads the same fixed size, so dragging stays correct. Length is
in canvas units (`Pixels` mode, converted to a fraction of the track each frame
so it survives resizing) or a fraction of the track. Every scrollbar in the
project needs the component: `Tools > Scrollbars > Add Fixed Handle To All
Scrollbars` adds it to project prefabs + open scenes (prefab-instance
scrollbars are skipped - they take it from their prefab asset). A new scrollbar
needs the component added by hand (or `Tools > Scrollbars > Add Fixed Handle To
Selected`).

## The tool hotbar rotates 3 fixed cards instead of sliding a track

`ToolHotbarUI` used to be a plain row of `EquipCapacity` (3) equal tiles with
a highlight on the equipped one. It's now a carousel: a large centered card
(the equipped/held tool) flanked by two small cards (the other two equip
slots), matching `item_slot`/`item_slot_small` art already in
`Assets/Sprites/UI/gameplay/`.

Unlike `ExpeditionStewSelectionUI`'s carousel (a track that slides
continuously through an arbitrarily long, growable list of bowls), this one
has exactly 3 possible contents, ever — the 3 equip slots. So a selection
change isn't "slide toward the newly centered item," it's a ROTATION of the
three already-visible icons between three FIXED screen positions: moving to
the next slot shifts every icon one card to the left, and the vacated right
card is filled by whatever just fell off the left (wraps around); moving to
the previous slot is the mirror. `ToolHotbarUI` tracks this with three plain
ints (which equip-slot index each position is currently showing) and rotates
them with a tuple assignment — no track, no continuous position, no
`LayoutGroup` needed. On top of the rotation, the center card's frame flashes
to an alternate sprite (`item_slot_change`) briefly, as the "now equipped"
cue.

This only has a defined shape for exactly 3 equip slots (`EquipCapacity`
changing away from 3 falls back to a plain snap, no rotation animation - see
`ToolHotbarUI.HandleEquippedChanged`). If equip capacity ever needs to grow,
this carousel needs a different design (more cards, or a sliding track like
the stew carousel), not a tweak.

## Number keys are bounded by EquipCapacity, not by the full 20-slot storage

`ToolEquipController.HandleNumberKeyInput` used to loop through all 10
number keys regardless of `EquipCapacity`, so e.g. key "4" could equip slot 3
even though only the first `EquipCapacity` (3) slots are ever shown in the
hotbar or meant to be equippable - the scroll wheel (`CycleEquipped`) was
already correctly bounded, just not the number keys. This was harmless
before (equipping an "invisible" slot just meant the hotbar didn't highlight
anything), but it breaks the new carousel's math outright: `ToolHotbarUI`
assumes the equipped index never leaves `[0, EquipCapacity)` so it can always
name the other two equip slots as "left" and "right." Fixed by binding only
as many number keys as there are equip slots.

## Creatures have hated scents (scentAversion), not just loved ones

`CreatureData.scentPreference` only ever pulled spawn weight up (creatures had
no way to be repelled by a scent). Added a parallel `scentAversion` array
(same 5-axis shape) plus a shared `CreatureData.GetScentAffinity(currentScents,
out loved, out hated)` helper, so `Spawner` and `CreatureAI` always read the
exact same match instead of each re-deriving it:

- **Spawn weight** (`Spawner.GetEffectiveWeight`): `weight *= max(0.05, 1 +
  loved - hated)`. The floor (0.05, not 0) is deliberate - a stew a species
  maximally hates should make it very rare, not literally unspawnable for the
  whole run (which could block bestiary completion with no recourse).
- **Detection radius** (`CreatureAI.EffectiveDetectionRadius`): multiplied by
  `1 + hatedScore * data.detectionAversionScale` (a new per-species tunable,
  default 1 = doubles at maximum aversion match). This is INDEPENDENT of
  Soothing Power's multiplier (a stew-modifier-type effect keyed by ingredient
  family dominance) - both stack, since they're driven by different things
  (family dominance vs. the 5-axis scent profile).

Flee speed is NOT affected by aversion (only detection range, per the ask) -
a natural follow-up if "hated scents make them flee faster too" is wanted
later. `scentAversion` defaults to all-zero on every existing `CreatureData`
asset (no aversion authored yet) - same as any newly-added serialized field -
so this is a no-op until someone fills it in per species. Note:
`scentPreference`'s values aren't actually clamped to 0-1 despite its old
tooltip claiming that (e.g. Alien.asset has 4 on one axis) - `scentAversion`
follows the same unbounded-positive-weight convention, not a hard 0-1 range.

## Stew scents: divided by cauldron capacity, not a fixed constant; 0-100 scale

Two related fixes to `StewCalculator.CalculateScents`:

1. **"1-2 ingredients reach max smell" fix.** The old formula was `1 + rawSum
   / scentPerPoint` (scentPerPoint a fixed constant, 3) - completely
   independent of how many total ingredient slots the cauldron has, so a
   couple of strong ingredients could already sum to a near-max raw value.
   Fixed by dividing by `scentPerPoint * totalCapacity` instead - using only a
   few ingredients out of the cauldron's TOTAL slot count (not how many are
   currently unlocked, and not how many are actually used, but the cauldron's
   real physical size - see below) can now only ever reach a small fraction of
   the scale, however strong those ingredients are; reaching a high value on
   one axis means deliberately filling most/all of the cauldron with
   ingredients strong on THAT axis (which also means NOT using those slots for
   other axes/modifiers - a real tradeoff). `StewCalculator.Calculate` now
   takes an explicit `totalCapacity` parameter rather than a new config field,
   so it's always the real array size (`BrewingStationUI.cauldronSlots.Length`)
   with no possibility of drifting out of sync with a separately-tuned config
   number.

   Deliberately uses the cauldron's TOTAL capacity (8), not
   `unlockedSlotCount` (starts at 4, grows via `UnlockSlot()`): using the
   currently-unlocked count would mean the exact same recipe reads as a
   WEAKER stew after the player unlocks more slots, purely because the
   denominator grew - a confusing "my old recipe got nerfed" surprise for no
   in-fiction reason. Using the fixed total means a recipe's scent is always
   computed the same way, and instead unlocking slots is what makes STRONGER
   recipes possible (a normal, legible incentive to unlock more slots).

2. **Scale changed from 1-5 to 0-100** (temporary, explicitly for balance
   visibility while tuning - a later pass will remap this to whatever's shown
   to the player). Neutral/baseline changed from 1 (the old floor) to 0 (no
   stew = no scent presence on ANY axis, `ExpeditionStewManager.GetScents()`'s
   no-stew default is now `[0,0,0,0,0]`) - this also means a stew with no
   ingredients contributing to a given axis now shows literal 0 there, not a
   nonzero "ambient" floor. Every scent consumer was updated to match:
   `Spawner`'s stew-scent normalization (`/100f`, was `/5f`),
   `ExpeditionStewManager.GetDefaultStew()`'s clamp range, and
   `StewDisplayUtil.SetScentTexts`'s format (whole numbers, not one decimal).

   `StewCalculationConfig.scentPerPoint`'s serialized value on
   `Assets/StewCalculationConfig.asset` was changed 3 -> 5 (its OLD value was
   tuned for the OLD 1-5/fixed-divisor formula and is meaningless under the
   new one) and `defaultStewScents` 1,1,1,1,1 -> 0,0,0,0,0 (matching the new
   neutral baseline) - both need real playtesting to retune properly, these
   are just sane starting points, not balanced values.

   The MODIFIER power calculation (`StewCalculator.CalculateModifier`, which
   picks ShinyPower/IngredientPower/etc. and its power 0-1) has the exact same
   "few ingredients reach 100%" characteristic (it divides by the USED
   ingredient count, so e.g. one Animal-family ingredient alone hits
   IngredientPower at full power) but was deliberately left untouched here -
   the ask was specifically about scent/"smell". Flagged as a likely-wanted
   follow-up, not done speculatively.

## Scent preference simplified to one favorite + one hated axis (dropdowns)

`CreatureData.scentPreference[5]`/`scentAversion[5]` (a weighted value per
axis) was replaced with a single `favoriteScent`/`hatedScent` pair, each a
plain `ScentType` enum (a dropdown in the Inspector - `Assets/Scripts/
Resources/ScentType.cs`, order Sweet/Fresh/Putrid/Metallic/Marine, matching
`StewInstance.scents`'s index order). A species now only ever reacts to
ONE stew scent axis for love and ONE for hate - the other three are ignored
entirely, even if the stew maxes them out. `ResourceData`'s five separate
scent fields are UNCHANGED (an ingredient still contributes to all five axes
at once - only the CREATURE side simplified).

No separate "strength" field was added - the match score is just the active
stew's value on that one axis, normalized 0-1 (`CreatureData.
GetScentAffinity`). This was a deliberate simplification, not an oversight:
the ask was explicitly "just a dropdown," and `detectionAversionScale`
(unrelated - controls how much a hated-scent match affects detection
specifically, independent of which axis is chosen) already gives per-species
tuning room. Add a per-species multiplier back if per-creature intensity
turns out to matter later.

`Spawner`/`CreatureAI` needed NO changes - both already went through
`CreatureData.GetScentAffinity()` (added in the previous "hated scents" pass
specifically so callers wouldn't need to know the internal representation),
so this simplification was fully contained to `CreatureData.cs`.

**Migration of the 8 existing CreatureData assets**: each had exactly one
clearly-dominant nonzero `scentPreference` axis, so `favoriteScent` was set to
match it losslessly (e.g. Alien's `[0,0,0,0,4]` -> Marine). `scentAversion`
had no real authored data (it was only just added, still all-zero on every
asset) - there was nothing to migrate FROM, so `hatedScent` was set to an
ARBITRARY placeholder (the next axis after favorite) purely so it doesn't
default to the same axis as favoriteScent (which would be a directly
contradictory "loves and hates Sweet" state for 4 of the 8 creatures, since
they all defaulted toward Sweet). Every creature's hatedScent still needs a
real, intentional choice from the designer.

## Modifier power also divided by cauldron capacity, not used-ingredient count

`StewCalculator.CalculateModifier` had the exact same shape of bug as the
scent calculation (see the entry above): every family-count and the rarity
sum were divided by `ingredients.Count` (how many were actually USED), so a
single matching ingredient reached ratio=1.0 - 100% modifier power - outright
(e.g. one Animal-family ingredient alone maxed out Ingredient Power; one
Legendary ingredient alone maxed out Shiny Power). Fixed the same way: divide
by the cauldron's TOTAL capacity (the same `totalCapacity` parameter
`Calculate` already threads through for scents) instead. Reaching 100% power
on any modifier now needs filling the ENTIRE cauldron with ingredients of one
family (or all at max rarity, for Shiny Power) - a deliberately rare, top-tier
outcome, matching "modifiers are supposed to be more rare."

`modifierPower`'s numeric range is UNCHANGED (still 0-1, displayed as `*100`
%) - unlike scents, this wasn't switched to a 0-100 scale, since `modifierPower`
feeds directly into half a dozen OTHER config-scale constants in
`ExpeditionStewManager` (`shinyRarityMultiplierScale`,
`ingredientDoubleChanceScale`, `encounterFamilyWeightScale`,
`chronoMaxBonusSeconds`, `captureChanceBonusScale`,
`captureMaxAutoBreakAreas`, `soothingMaxSlowdown`) that all assume a 0-1
input - rescaling would have meant retuning every one of them too, which
wasn't asked for. Only the denominator changed.

Practical consequence worth knowing: with the live config's `modifierActivationThreshold`
(0.3) and the cauldron's real capacity (8), a modifier now needs AT LEAST 3
ingredients of the same family (`0.3 * 8 = 2.4`, rounds up) in the pot before
any modifier activates at all - fewer than that and every brew is
`StewModifierType.None`. This wasn't retuned; it's a direct, intentional
consequence of the capacity-based fix, called out so it isn't mistaken for a
new bug.
