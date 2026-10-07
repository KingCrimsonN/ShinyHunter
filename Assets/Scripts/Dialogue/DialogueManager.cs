using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Persistent dialogue runner - one instance survives across scenes
/// (DontDestroyOnLoad), so any NPCDialogueTrigger in any scene can call
/// StartDialogue() on it. Player references (FirstPersonController,
/// Interactor) are looked up fresh each time a dialogue starts rather than
/// serialized, since the player object is scene-specific while this manager
/// is not.
///
/// Interactor.cs itself is untouched - this manager stops the player from
/// re-triggering an interaction mid-conversation by disabling the Interactor
/// component directly, not by changing its code.
/// </summary>
public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    [Header("UI - Popup")]
    [SerializeField] private GameObject popupRoot;
    [SerializeField] private Image npcIcon;
    [SerializeField] private TMP_Text npcNameText;
    [SerializeField] private TMP_Text lineText;
    [Tooltip("Optional - e.g. a small blinking arrow shown once a line is fully typed, hidden while typing or while choices are shown.")]
    [SerializeField] private GameObject continueIndicator;

    [Header("UI - Choices")]
    [SerializeField] private Button choiceButtonPrefab;
    [SerializeField] private Transform choiceButtonParent;

    [Header("Typewriter")]
    [SerializeField] private float charactersPerSecond = 40f;

    [Header("Camera")]
    [Tooltip("Seconds the camera takes to turn toward the NPC when a dialogue starts (only for NPCs that pass a focus point - see StartDialogue).")]
    [SerializeField] private float cameraFocusDuration = 0.6f;

    /// <summary>Fired when a dialogue opens.</summary>
    public event Action OnDialogueStarted;
    /// <summary>Fired when a dialogue closes, for any reason.</summary>
    public event Action OnDialogueEnded;
    /// <summary>Fired when a choice with a non-empty actionId is selected. Listen for this to trigger custom functionality (see DialogueActionRouter).</summary>
    public event Action<string> OnChoiceAction;

    public bool IsActive { get; private set; }

    private DialogueData currentData;
    private DialogueNodeData currentNode;
    private int currentLineIndex;
    private bool isTyping;
    private bool awaitingChoice;
    private Coroutine typingCoroutine;

    /// <summary>Frame a choice was last selected on - Update ignores advance input on that same frame, so the click/Space that picked a choice can't also skip the first line of the node it leads to.</summary>
    private int choiceFrame = -1;

    private readonly List<GameObject> spawnedChoiceButtons = new List<GameObject>();

    /// <summary>
    /// Every DialogueData the player has already started once this session -
    /// decides startNode (first time) vs repeatNode (after). Kept here, not on
    /// the shared DialogueData asset (CLAUDE.md #1). Not saved to disk: there
    /// is no save system yet, so every new session replays introductions.
    /// </summary>
    private readonly HashSet<DialogueData> startedDialogues = new HashSet<DialogueData>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (popupRoot != null) popupRoot.SetActive(false);
        choiceButtonParent.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (!IsActive || awaitingChoice || Time.frameCount == choiceFrame) return;

        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space))
        {
            if (isTyping) CompleteTypewriter();
            else AdvanceLine();
        }
    }

    /// <summary>True if this conversation has already been started once - the next StartDialogue will use its repeatNode (if it has one).</summary>
    public bool HasStarted(DialogueData data) => data != null && startedDialogues.Contains(data);

    /// <summary>Forgets that a conversation happened, so its startNode (the intro) plays again next time. For debugging / story resets.</summary>
    public void ResetStarted(DialogueData data)
    {
        if (data != null) startedDialogues.Remove(data);
    }

    /// <summary>
    /// Where to aim the camera for an NPC: an explicitly placed head transform
    /// if it has one, otherwise roughly head height from its collider (the
    /// root transform usually sits at the feet, which is what left the camera
    /// staring at the floor).
    /// </summary>
    public static Vector3 GetHeadPoint(Transform npc, Transform explicitHead)
    {
        if (explicitHead != null) return explicitHead.position;

        var collider = npc.GetComponentInChildren<Collider>();
        if (collider != null)
        {
            Bounds b = collider.bounds;
            return b.center + Vector3.up * (b.extents.y * 0.75f);
        }

        return npc.position + Vector3.up * 1.6f;
    }

    /// <summary>
    /// Entry point - call from an IInteractable's Interact() (see NPCDialogueTrigger).
    /// If focusPoint is given, the player's camera smoothly turns to look at it
    /// (see FirstPersonController.FocusOn); leave it null for dialogues that
    /// shouldn't move the camera.
    /// </summary>
    public void StartDialogue(DialogueData data, Vector3? focusPoint = null)
    {
        if (data == null || IsActive) return;

        // First time: startNode (the intro). Every time after: repeatNode, if
        // one is set - otherwise startNode again, exactly as before this existed.
        var entryNode = HasStarted(data) && data.repeatNode != null ? data.repeatNode : data.startNode;
        if (entryNode == null) return;

        // Marked as soon as it begins, not when it finishes: the player can't
        // walk away mid-conversation (they're frozen), so there is no
        // "started the intro but never saw it" case to protect against.
        startedDialogues.Add(data);

        currentData = data;
        IsActive = true;

        if (npcIcon != null)
        {
            npcIcon.sprite = data.npcIcon;
            npcIcon.enabled = data.npcIcon != null;
        }
        if (npcNameText != null) npcNameText.text = data.npcName;

        PlayerStateManager.Instance.Freeze();

        if (focusPoint.HasValue)
        {
            // Looked up fresh like the rest of the player state - the player object isn't persistent.
            var player = FindFirstObjectByType<FirstPersonController>();
            if (player != null) player.FocusOn(focusPoint.Value, cameraFocusDuration);
        }

        if (popupRoot != null) popupRoot.SetActive(true);

        OnDialogueStarted?.Invoke();
        SetNode(entryNode);
    }

    private void SetNode(DialogueNodeData node)
    {
        currentNode = node;
        currentLineIndex = 0;
        awaitingChoice = false;
        ClearChoiceButtons();

        ShowLine();
    }

    private void ShowLine()
    {
        if (currentNode.lines == null || currentLineIndex >= currentNode.lines.Length)
        {
            HandleNodeFinished();
            return;
        }

        if (continueIndicator != null) continueIndicator.SetActive(false);

        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        typingCoroutine = StartCoroutine(TypeLine(currentNode.lines[currentLineIndex]));
    }

    private void AdvanceLine()
    {
        currentLineIndex++;
        ShowLine();
    }

    private void HandleNodeFinished()
    {
        if (currentNode.choices != null && currentNode.choices.Count > 0)
        {
            ShowChoices(currentNode.choices);
        }
        else if (currentNode.defaultNextNode != null)
        {
            SetNode(currentNode.defaultNextNode); // no decision needed - auto-continue
        }
        else
        {
            EndDialogue();
        }
    }

    private void ShowChoices(List<DialogueChoice> choices)
    {
        awaitingChoice = true;
        if (continueIndicator != null) continueIndicator.SetActive(false);
        choiceButtonParent.gameObject.SetActive(true);

        foreach (var choice in choices)
        {
            var button = Instantiate(choiceButtonPrefab, choiceButtonParent);

            var label = button.GetComponentInChildren<TMP_Text>();
            if (label != null) label.text = choice.label;

            button.onClick.AddListener(() => SelectChoice(choice));
            spawnedChoiceButtons.Add(button.gameObject);
        }
    }

    private void SelectChoice(DialogueChoice choice)
    {
        ClearChoiceButtons();
        awaitingChoice = false;
        choiceFrame = Time.frameCount;

        // Resolve the dialogue's OWN state change (continue or end) BEFORE
        // firing the action event. If we fired the action first, an action
        // that opens another popup and freezes the player would have that
        // freeze immediately undone by EndDialogue()'s own unfreeze below.
        // Doing it in this order means whichever system freezes last wins.
        if (choice.nextNode != null)
            SetNode(choice.nextNode);
        else
            EndDialogue();

        if (!string.IsNullOrEmpty(choice.actionId))
            OnChoiceAction?.Invoke(choice.actionId);
    }

    private void EndDialogue()
    {
        IsActive = false;

        if (popupRoot != null) popupRoot.SetActive(false);
        ClearChoiceButtons();
        PlayerStateManager.Instance.Unfreeze();

        currentData = null;
        currentNode = null;

        OnDialogueEnded?.Invoke();
    }

    private void ClearChoiceButtons()
    {
        foreach (var btn in spawnedChoiceButtons)
            if (btn != null) Destroy(btn);

        spawnedChoiceButtons.Clear();
    }

    /// <summary>
    /// Typewriter via TMP's maxVisibleCharacters: the full line is assigned
    /// ONCE and characters are revealed by raising the visible count. The old
    /// version did `text += c` per character, which made TMP re-parse the
    /// whole string and rebuild its mesh dozens of times a second - with a
    /// dynamic font atlas that repeatedly triggered glyph population / sub-mesh
    /// creation mid-rebuild (the TMP_SubMeshUI "Assertion failed" spam). As a
    /// bonus the layout is final from the first frame, so words no longer
    /// jump to the next line while a line is being typed.
    /// </summary>
    private IEnumerator TypeLine(string line)
    {
        isTyping = true;

        int total = line.Length;
        if (lineText != null)
        {
            lineText.text = line;
            lineText.maxVisibleCharacters = 0;
            lineText.ForceMeshUpdate();
            total = lineText.textInfo.characterCount; // visible characters only - rich-text tags don't count
        }

        float delay = charactersPerSecond > 0f ? 1f / charactersPerSecond : 0f;

        for (int visible = 1; visible <= total; visible++)
        {
            if (lineText != null) lineText.maxVisibleCharacters = visible;
            if (delay > 0f) yield return new WaitForSeconds(delay);
            else yield return null;
        }

        if (lineText != null) lineText.maxVisibleCharacters = int.MaxValue;
        isTyping = false;
        typingCoroutine = null;

        if (continueIndicator != null) continueIndicator.SetActive(true);
    }

    private void CompleteTypewriter()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        if (lineText != null && currentNode != null && currentLineIndex < currentNode.lines.Length)
        {
            lineText.text = currentNode.lines[currentLineIndex];
            lineText.maxVisibleCharacters = int.MaxValue;
        }

        isTyping = false;
        if (continueIndicator != null) continueIndicator.SetActive(true);
    }
}