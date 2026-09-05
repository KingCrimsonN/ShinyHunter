using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// One "page" of a conversation: lines typed out in order, then either
/// player-facing choices, an automatic continuation, or the end of the
/// conversation. Nodes are separate assets (not nested classes) so they can
/// reference each other as nextNode without any serialization depth issues.
/// </summary>
[CreateAssetMenu(fileName = "NewDialogueNode", menuName = "ShinyHunt/Dialogue Node")]
public class DialogueNodeData : ScriptableObject
{
    [TextArea(2, 4)]
    public string[] lines;

    public List<DialogueChoice> choices = new List<DialogueChoice>();

    [Tooltip("Used ONLY if choices is empty: once all lines are shown, dialogue automatically continues here. Leave null to end the conversation after this node's lines.")]
    public DialogueNodeData defaultNextNode;
}

[System.Serializable]
public class DialogueChoice
{
    public string label;

    [Tooltip("Optional. Identifies custom functionality for this choice (e.g. \"OpenTransformStation\", \"ClaimMoney\") - dispatched via DialogueManager.OnChoiceAction. Leave blank if this choice should just progress or end the dialogue with no side effect.")]
    public string actionId;

    [Tooltip("Optional. Which node to continue to after this choice. Leave null to end the conversation (e.g. an \"Exit\" choice), or point back at the node this choice lives on to create a repeating menu (e.g. \"Claim Money\" looping back to the same options).")]
    public DialogueNodeData nextNode;
}
