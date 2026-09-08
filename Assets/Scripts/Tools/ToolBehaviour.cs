using UnityEngine;

/// <summary>
/// Base class for a tool's in-hand behaviour. Subclass this per tool type
/// (e.g. StickTool, SealTool, RepellentTool) and put the subclass on the
/// root of that tool's prefab, then assign the prefab to the matching
/// ToolData.toolPrefab.
///
/// ToolEquipController instantiates this prefab under the player's hand
/// socket whenever this tool becomes equipped, and calls UseTool() on
/// primary input (left click by default).
/// </summary>
public class ToolBehaviour : MonoBehaviour
{
    /// <summary>Called once, right after this instance is spawned into the hand socket.</summary>
    public virtual void OnEquip() { }

    /// <summary>Called right before this instance is destroyed (switching to another tool).</summary>
    public virtual void OnUnequip() { }

    /// <summary>Primary use input while this tool is equipped and held.</summary>
    public virtual void UseTool() { }

    public virtual void Update() { }

    public Vector3 offset;
}
