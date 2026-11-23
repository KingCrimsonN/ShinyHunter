using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// The CritterDex as a BOOK. Page 0 is the index (the grid of every critter);
/// pages 1..N are one critter each, in book order - grouped by family in the
/// registry's familyOrder (Bugs, Plants, Freaky, Warm, Cold), registry order
/// within a family. So adding a critter is just adding it to the registry.
///
/// Ways to turn pages: clicking an index entry (jump to that critter),
/// Next/PreviousPage (the side arrows), and GoToIndex/GoToFamily (the
/// bookmarks - see CritterDexBookmarksUI). Every change plays the same page
/// flip: the page container squashes horizontally to nothing, the content
/// swaps at the midpoint, then it opens back up. Put the container's pivot X
/// on the book's spine for it to read as a page turning.
///
/// Lives on the dex tablet page's root, so OnEnable fires every time the page
/// is opened - it always reopens on the index (unless Reopen On Last Page).
/// Owns the ordering: the index grid is handed the ordered list rather than
/// reading the registry itself, so the two can never disagree.
/// </summary>
public class CritterDexUI : MonoBehaviour
{
    [SerializeField] private CritterDexRegistry registry;
    [SerializeField] private CritterDexGridUI gridUI;
    [SerializeField] private CritterDexDetailUI detailUI;

    [Header("Pages")]
    [Tooltip("The index page object - contains the grid. Shown on page 0 only.")]
    [SerializeField] private GameObject indexPage;
    [Tooltip("The critter page object - contains CritterDexDetailUI. Shown on every page except the index.")]
    [SerializeField] private GameObject critterPage;

    [Header("Page Flip")]
    [Tooltip("What gets squashed during a flip: the parent of both page objects, NOT the arrows/bookmarks. Set its pivot X to the spine (usually 0.5). Leave empty for instant page changes.")]
    [SerializeField] private RectTransform pageContainer;
    [Tooltip("Total seconds for a flip (half closing, half opening). 0 = no animation.")]
    [SerializeField] private float flipDuration = 0.35f;
    [SerializeField] private AudioClip flipSound;
    [SerializeField, Range(0f, 1f)] private float flipSoundVolume = 0.5f;

    [Header("Page Number")]
    [Tooltip("Optional - the page number in the book's corner. Put it outside the two page objects (on the book) so it shows on every page.")]
    [SerializeField] private TMPro.TMP_Text pageNumberText;
    [Tooltip("What the index page shows instead of a number (e.g. a Roman \"I\").")]
    [SerializeField] private string indexPageLabel = "I";
    [Tooltip("Number format on critter pages - \"00\" = 01, 02... The number is the critter's dex number (its position in the book).")]
    [SerializeField] private string pageNumberFormat = "00";

    [Header("Arrows")]
    [Tooltip("Hidden on the index page (nothing before it).")]
    [SerializeField] private GameObject previousArrow;
    [Tooltip("Hidden on the last critter's page.")]
    [SerializeField] private GameObject nextArrow;

    [Header("Behaviour")]
    [Tooltip("Off (default): opening the dex always starts on the index. On: it reopens on whichever page was open last.")]
    [SerializeField] private bool reopenOnLastPage = false;

    [Header("Input (Input System)")]
    [Tooltip("Optional: an action from an Input Actions asset for 'previous page'. Empty = built-in bindings: Left Arrow, A, gamepad D-pad left, left shoulder.")]
    [SerializeField] private InputActionReference previousPageAction;
    [Tooltip("Optional: an action for 'next page'. Empty = built-in bindings: Right Arrow, D, gamepad D-pad right, right shoulder.")]
    [SerializeField] private InputActionReference nextPageAction;
    [Tooltip("Optional: an action for 'back to the index'. Empty = built-in bindings: Up Arrow, W, Backspace, gamepad D-pad up.")]
    [SerializeField] private InputActionReference indexPageAction;

    private InputAction previousInput;
    private InputAction nextInput;
    private InputAction indexInput;
    /// <summary>The built-in fallback actions - ours to enable/disable/dispose. Asset actions are only borrowed.</summary>
    private readonly HashSet<InputAction> ownedInputActions = new HashSet<InputAction>();

    private List<CreatureData> ordered;
    private int currentPage;          // the page actually on screen
    private int targetPage;           // where we're heading (differs from currentPage only mid-flip)
    private bool flipping;
    private Sequence flipSequence;
    private Vector3 containerBaseScale = Vector3.one;

    /// <summary>Fires after a page is actually shown (mid-flip, at the moment the content swaps), with its page number.</summary>
    public event Action<int> OnPageChanged;

    public bool IsIndexPage => currentPage == 0;
    public int PageCount => EnsureOrdered().Count + 1;

    /// <summary>The family of the critter page being shown, or null on the index.</summary>
    public IngredientFamily? CurrentFamily
    {
        get
        {
            var list = EnsureOrdered();
            return currentPage > 0 && currentPage <= list.Count ? list[currentPage - 1].family : (IngredientFamily?)null;
        }
    }

    private void Awake()
    {
        if (pageContainer != null) containerBaseScale = pageContainer.localScale;
        SetupInput();
    }

    private void OnDestroy()
    {
        foreach (var action in ownedInputActions) action.Dispose();
        ownedInputActions.Clear();
    }

    /// <summary>
    /// Page turning from keyboard/gamepad, through the Input System so controller
    /// support only needs bindings, not code. Each action is either the assigned
    /// asset action or a built-in one with keyboard + gamepad bindings.
    /// </summary>
    private void SetupInput()
    {
        previousInput = Resolve(previousPageAction, "DexPreviousPage", "<Keyboard>/leftArrow", "<Keyboard>/a", "<Gamepad>/dpad/left", "<Gamepad>/leftShoulder");
        nextInput = Resolve(nextPageAction, "DexNextPage", "<Keyboard>/rightArrow", "<Keyboard>/d", "<Gamepad>/dpad/right", "<Gamepad>/rightShoulder");
        indexInput = Resolve(indexPageAction, "DexIndexPage", "<Keyboard>/upArrow", "<Keyboard>/w", "<Keyboard>/backspace", "<Gamepad>/dpad/up");
    }

    private InputAction Resolve(InputActionReference reference, string name, params string[] fallbackBindings)
    {
        if (reference != null && reference.action != null) return reference.action;

        var action = new InputAction(name, InputActionType.Button);
        foreach (var binding in fallbackBindings) action.AddBinding(binding);
        ownedInputActions.Add(action);
        return action;
    }

    private void SubscribeInput(bool subscribe)
    {
        foreach (var (action, handler) in new (InputAction, Action<InputAction.CallbackContext>)[]
                 { (previousInput, OnPreviousInput), (nextInput, OnNextInput), (indexInput, OnIndexInput) })
        {
            if (action == null) continue;
            if (subscribe)
            {
                action.performed += handler;
                action.Enable(); // an asset action may be shared - enabling it again is harmless
            }
            else
            {
                action.performed -= handler;
                if (ownedInputActions.Contains(action)) action.Disable(); // never disable a borrowed asset action others may use
            }
        }
    }

    private void OnPreviousInput(InputAction.CallbackContext _) => PreviousPage();
    private void OnNextInput(InputAction.CallbackContext _) => NextPage();
    private void OnIndexInput(InputAction.CallbackContext _) => GoToIndex();

    private void OnEnable()
    {
        Rebuild();

        if (gridUI != null) gridUI.OnSpeciesClicked += HandleSpeciesClicked;
        SubscribeInput(true);

        int start = reopenOnLastPage ? Mathf.Clamp(currentPage, 0, PageCount - 1) : 0;
        targetPage = start;
        ApplyPage(start);
    }

    private void OnDisable()
    {
        if (gridUI != null) gridUI.OnSpeciesClicked -= HandleSpeciesClicked;
        SubscribeInput(false);

        // The tablet closing mid-flip must not leave the pages squashed flat.
        if (flipSequence != null) flipSequence.Kill();
        flipSequence = null;
        flipping = false;
        if (pageContainer != null) pageContainer.localScale = containerBaseScale;
    }

    // ---------------- Navigation ----------------

    public void GoToIndex() => GoToPage(0);
    public void NextPage() => GoToPage(targetPage + 1);
    public void PreviousPage() => GoToPage(targetPage - 1);

    public void GoToSpecies(CreatureData species)
    {
        int index = EnsureOrdered().IndexOf(species);
        if (index >= 0) GoToPage(index + 1);
    }

    /// <summary>Flips to the FIRST critter page of a family. Does nothing if no critter of that family is in the registry.</summary>
    public void GoToFamily(IngredientFamily family)
    {
        int index = EnsureOrdered().FindIndex(s => s.family == family);
        if (index >= 0) GoToPage(index + 1);
    }

    public bool HasFamily(IngredientFamily family)
    {
        return EnsureOrdered().Exists(s => s.family == family);
    }

    public void GoToPage(int page)
    {
        page = Mathf.Clamp(page, 0, PageCount - 1);
        if (page == targetPage) return;

        targetPage = page;
        if (!flipping) StartFlip();
    }

    private void HandleSpeciesClicked(CreatureData species)
    {
        GoToSpecies(species);
    }

    // ---------------- Flip ----------------

    private void StartFlip()
    {
        if (currentPage == targetPage) return;

        if (pageContainer == null || flipDuration <= 0f || !isActiveAndEnabled)
        {
            ApplyPage(targetPage);
            return;
        }

        flipping = true;
        PlayFlipSound();

        // SetUpdate(true): the tablet freezes the game with Time.timeScale = 0
        // (UIManager.LockPlayer), which would stall a normal tween forever.
        // The content swap reads targetPage AT the midpoint, so extra clicks
        // during the first half simply retarget this same flip; clicks during
        // the second half start another flip when this one finishes.
        float half = flipDuration * 0.5f;
        ApplyPage(targetPage);
        FinishFlip();
        // flipSequence = DOTween.Sequence().SetUpdate(true)
        //     .Append(pageContainer.DOScaleX(0f, half).SetEase(Ease.InQuad))
        //     .AppendCallback(() => ApplyPage(targetPage))
        //     .Append(pageContainer.DOScaleX(containerBaseScale.x, half).SetEase(Ease.OutQuad))
        //     .OnComplete(FinishFlip);
    }

    private void FinishFlip()
    {
        flipping = false;
        flipSequence = null;
        if (currentPage != targetPage) StartFlip();
    }

    private void PlayFlipSound()
    {
        if (flipSound != null && SoundFXManager.instance != null)
            SoundFXManager.instance.PlaySoundFX(flipSound, transform, flipSoundVolume);
    }

    // ---------------- Showing a page ----------------

    private void ApplyPage(int page)
    {
        currentPage = page;
        bool isIndex = page == 0;

        if (indexPage != null) indexPage.SetActive(isIndex);
        if (critterPage != null) critterPage.SetActive(!isIndex);

        if (detailUI != null)
        {
            var list = EnsureOrdered();
            if (isIndex || page > list.Count) detailUI.Clear();
            else detailUI.Show(list[page - 1]);
        }

        if (pageNumberText != null) pageNumberText.text = isIndex ? indexPageLabel : page.ToString(pageNumberFormat);

        if (previousArrow != null) previousArrow.SetActive(page > 0);
        if (nextArrow != null) nextArrow.SetActive(page < PageCount - 1);

        OnPageChanged?.Invoke(page);
    }

    // ---------------- Ordering ----------------

    private List<CreatureData> EnsureOrdered()
    {
        if (ordered == null) Rebuild();
        return ordered;
    }

    private void Rebuild()
    {
        if (registry == null)
        {
            Debug.LogError($"CritterDexUI on '{name}': no CritterDexRegistry assigned - the book will only have an empty index.", this);
            ordered = new List<CreatureData>();
        }
        else
        {
            ordered = registry.GetOrderedSpecies();
        }

        if (gridUI != null) gridUI.SetSpecies(ordered);
    }
}
