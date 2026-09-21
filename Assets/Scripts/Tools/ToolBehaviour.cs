using System;
using UnityEngine;

/// <summary>
/// Base class for a tool's in-hand behaviour. Subclass this per tool type
/// (e.g. StickTool, VoodooDoll, RepellentTool) and put the subclass on the
/// root of that tool's prefab, then assign the prefab to the matching
/// ToolData.toolPrefab.
///
/// ToolEquipController instantiates this prefab under the player's hand
/// socket whenever this tool becomes equipped, and calls UseTool() on
/// primary input (left click by default).
/// </summary>
public class ToolBehaviour : MonoBehaviour
{
    public Vector3 offset;

    /// <summary>
    /// Fired whenever this tool's current use should count as consuming one
    /// unit from the inventory stack (only actually removes anything if
    /// this tool's ToolData.consumable is true). Call RaiseConsumed() from
    /// a subclass at whatever point the use becomes "committed" - this can
    /// be synchronous inside UseTool(), or much later, e.g. at the end of a
    /// coroutine - so a long-running tool action never has its own
    /// GameObject destroyed out from under it mid-use.
    /// </summary>
    public event Action OnConsumed;

    protected void RaiseConsumed() => OnConsumed?.Invoke();

    /// <summary>Called once, right after this instance is spawned into the hand socket.</summary>
    public virtual void OnEquip() { }

    /// <summary>Called right before this instance is destroyed (switching to another tool).</summary>
    public virtual void OnUnequip() { }

    /// <summary>Primary use input while this tool is equipped and held.</summary>
    public virtual void UseTool() { }

    /// <summary>
    /// Called every frame by ToolEquipController while this tool is equipped
    /// and usable. Deliberately NOT named Update: a method with that name on a
    /// MonoBehaviour is also called by Unity itself, so it would run twice per
    /// frame (and keep running while the player is frozen).
    /// </summary>
    public virtual void OnHeldUpdate() { }
}