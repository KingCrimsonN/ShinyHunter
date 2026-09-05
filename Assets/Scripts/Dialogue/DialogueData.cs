using UnityEngine;

/// <summary>
/// One conversation with one NPC/object - identity plus the entry point
/// into its node graph.
/// </summary>
[CreateAssetMenu(fileName = "NewDialogue", menuName = "ShinyHunt/Dialogue Data")]
public class DialogueData : ScriptableObject
{
    public string npcName;
    public Sprite npcIcon;
    public DialogueNodeData startNode;
}
