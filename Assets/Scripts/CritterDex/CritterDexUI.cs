using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

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

    [Header("Arrows")]
    [Tooltip("Hidden on the index page (nothing before it).")]
    [SerializeField] private GameObject previousArrow;
    [Tooltip("Hidden on the last critter's page.")]
    [SerializeField] private GameObject nextArrow;

    [Header("Behaviour")]
    [Tooltip("Off (default): opening the dex always starts on the index. On: it reopens on whichever page was open last.")]
    [SerializeField] private bool reopenOnLastPage = false;

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
    }

    private void OnEnable()
    {
        Rebuild();

        if (gridUI != null) gridUI.OnSpeciesClicked += HandleSpeciesClicked;

        int start = reopenOnLastPage ? Mathf.Clamp(currentPage, 0, PageCount - 1) : 0;
        targetPage = start;
        ApplyPage(start);
    }

    private void OnDisable()
    {
        if (gridUI != null) gridUI.OnSpeciesClicked -= HandleSpeciesClicked;

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
