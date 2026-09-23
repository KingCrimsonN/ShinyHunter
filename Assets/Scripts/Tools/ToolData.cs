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

    [Header("Consumption")]
    [Tooltip("If true, one is removed from the stack each time this tool's UseTool() reports a consuming use. Set false for reusable tools like a basic stick.")]
    public bool consumable = true;

    [Header("Behaviour")]
    [Tooltip("Prefab instantiated in the player's hand socket when this tool is equipped. Must have a ToolBehaviour (or subclass) component on its root.")]
    public ToolBehaviour toolPrefab;

    // Only meaningful for tools that trigger the capture minigame (currently
    // just the voodoo doll) - inert defaults for anything else. See
    // CaptureMinigameController and CreatureAI.TryCapture. Barrier COUNT/SIZE
    // and WHETHER they move are rarity-driven instead - see CaptureMinigameConfig.
    [Header("Capture Minigame")]
    [Tooltip("Seconds the player has to finish the capture minigame when using this tool.")]
    public float minigameTimeLimit = 5f;
    [Tooltip("Number of hit attempts (needle/nail marks) the minigame gives when using this tool - independent of how many barriers there are (CaptureMinigameConfig.barrierCountPerRarity), so a generous tool can give more swings than there are barriers to hit.")]
    [Min(1)] public int minigameAttemptCount = 3;
    [Tooltip("Multiplies moving barriers' orbit speed (only relevant for rarities where they move at all - CaptureMinigameConfig.barriersMovePerRarity) when using this tool. 1 = no change.")]
    public float barrierSpeedMultiplier = 1f;

    [Header("Capture Minigame - Extra Modifiers")]
    [Tooltip("Chance (0-1) a SUCCESSFUL capture with this tool is flagged double-yield (the same sparkle mechanic Ingredient Power uses - see InventoryManager.AddCreature's doubleYield parameter). Rolled independently of, and in addition to, any stew Ingredient Power roll - either succeeding is enough.")]
    [Range(0f, 1f)] public float extraIngredientChance = 0f;
    [Tooltip("Which family this tool is specially good at capturing. Ignored (no effect) while bonusFamilyCaptureChance is 0.")]
    public IngredientFamily bonusFamily = IngredientFamily.Bug;
    [Tooltip("Flat capture-chance bonus (0-1) added to the minigame's final ratio when the targeted creature's family matches bonusFamily. Stacks additively with any stew Capture Power bonus.")]
    [Range(0f, 1f)] public float bonusFamilyCaptureChance = 0f;
}