using UnityEngine;

/// <summary>
/// One conversation with one NPC/object - identity plus the entry points
/// into its node graph: startNode the FIRST time the player talks to them,
/// repeatNode every time after that.
///
/// Like every ScriptableObject here this is a shared asset, so "has the
/// player already talked to this NPC" is NOT stored on it - DialogueManager
/// keeps that (see DialogueManager.HasStarted).
/// </summary>
[CreateAssetMenu(fileName = "NewDialogue", menuName = "ShinyHunt/Dialogue Data")]
public class DialogueData : ScriptableObject
{
    public string npcName;
    public Sprite npcIcon;

    [Tooltip("Where the conversation begins the FIRST time - e.g. an introduction.")]
    public DialogueNodeData startNode;

    [Tooltip("Optional. Where the conversation begins on EVERY interaction after the first (e.g. just the \"What do you need?\" menu, with no intro). Leave empty to replay startNode every time.")]
    public DialogueNodeData repeatNode;
}
