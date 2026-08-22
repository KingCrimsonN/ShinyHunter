using UnityEngine;

/// <summary>
/// Per-tool configuration. Create one asset per tool/item type
/// (right click in Project window -> Create -> ShinyHunt -> Tool Data).
/// </summary>
[CreateAssetMenu(fileName = "NewTool", menuName = "ShinyHunt/Tool Data")]
public class ToolData : ScriptableObject
{
    [Header("Identity")]
    public string toolName;
    public Sprite icon;
    [TextArea] public string description;

    [Header("Stacking")]
    [Tooltip("Max amount of this item that can sit in a single inventory slot. Use 1 for non-stackable tools like the stick.")]
    [Min(1)] public int maxStack = 1;

    [Header("Behaviour")]
    [Tooltip("Prefab instantiated in the player's hand socket when this tool is equipped. Must have a ToolBehaviour (or subclass) component on its root.")]
    public ToolBehaviour toolPrefab;
}
