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
