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

    private readonly List<GameObject> spawnedChoiceButtons = new List<GameObject>();

    private FirstPersonController cachedPlayerMovement;
    private Interactor cachedInteractor;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        // DontDestroyOnLoad(gameObject);

        if (popupRoot != null) popupRoot.SetActive(false);
    }

    private void Update()
    {
        if (!IsActive || awaitingChoice) return;

        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space))
        {
            if (isTyping) CompleteTypewriter();
            else AdvanceLine();
        }
    }

    /// <summary>Entry point - call from an IInteractable's Interact() (see NPCDialogueTrigger).</summary>
    public void StartDialogue(DialogueData data)
    {
        if (data == null || data.startNode == null || IsActive) return;

        currentData = data;
        IsActive = true;

        if (npcIcon != null)
        {
            npcIcon.sprite = data.npcIcon;
            npcIcon.enabled = data.npcIcon != null;
        }
        if (npcNameText != null) npcNameText.text = data.npcName;

        FreezePlayer();

        if (popupRoot != null)
        {
            print("Showing Dialogue");
            popupRoot.SetActive(true);
        }

        OnDialogueStarted?.Invoke();
        SetNode(data.startNode);
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
        UnfreezePlayer();

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

    private IEnumerator TypeLine(string line)
    {
        isTyping = true;
        if (lineText != null) lineText.text = string.Empty;

        float delay = charactersPerSecond > 0f ? 1f / charactersPerSecond : 0f;

        foreach (char c in line)
        {
            if (lineText != null) lineText.text += c;
            if (delay > 0f) yield return new WaitForSeconds(delay);
            else yield return null;
        }

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
            lineText.text = currentNode.lines[currentLineIndex];

        isTyping = false;
        if (continueIndicator != null) continueIndicator.SetActive(true);
    }

    private void FreezePlayer()
    {
        cachedPlayerMovement = FindFirstObjectByType<FirstPersonController>();
        cachedInteractor = FindFirstObjectByType<Interactor>();

        if (cachedPlayerMovement != null) cachedPlayerMovement.enabled = false;
        if (cachedInteractor != null) cachedInteractor.enabled = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void UnfreezePlayer()
    {
        if (cachedPlayerMovement != null) cachedPlayerMovement.enabled = true;
        if (cachedInteractor != null) cachedInteractor.enabled = true;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}
