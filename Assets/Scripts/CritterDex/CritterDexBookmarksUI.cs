using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// The bookmarks down the side of the book: the first one flips to the index,
/// and there is one per critter family that flips to that family's FIRST
/// critter page. Replaces the old family-filter buttons - the buttons are
/// wired in CODE from the lists below, so there are no OnClick entries to set
/// up in the inspector (and none should be left over from the filter version).
///
/// Adding a family later = one more entry in Family Bookmarks (plus the family
/// in the registry's familyOrder). A bookmark whose family has no critters in
/// the registry is made non-interactable rather than removed, so the layout
/// doesn't shift.
/// </summary>
public class CritterDexBookmarksUI : MonoBehaviour
{
    [Serializable]
    public class IndexBookmark
    {
        public Button button;
        [Tooltip("Optional - shown while the index page is open (e.g. a 'pulled out' version of the tab).")]
        public GameObject activeMarker;
    }

    [Serializable]
    public class FamilyBookmark
    {
        public IngredientFamily family;
        public Button button;
        [Tooltip("Optional - shown while a page of this family is open.")]
        public GameObject activeMarker;
    }

    [SerializeField] private CritterDexUI book;
    [SerializeField] private IndexBookmark indexBookmark = new IndexBookmark();
    [Tooltip("One per family, any order (the visual order is your layout). Bug, Plant, Freaky, Warm, Cold.")]
    [SerializeField] private FamilyBookmark[] familyBookmarks = new FamilyBookmark[0];

    private readonly List<(Button button, UnityAction action)> wired = new List<(Button, UnityAction)>();

    private void OnEnable()
    {
        if (book == null)
        {
            Debug.LogError($"CritterDexBookmarksUI on '{name}': no CritterDexUI (the book) assigned.", this);
            return;
        }

        Wire(indexBookmark.button, book.GoToIndex);
        foreach (var bookmark in familyBookmarks)
        {
            var family = bookmark.family; // captured per-iteration for the closure
            Wire(bookmark.button, () => book.GoToFamily(family));
        }

        book.OnPageChanged += HandlePageChanged;
        Refresh();
    }

    private void OnDisable()
    {
        if (book != null) book.OnPageChanged -= HandlePageChanged;

        foreach (var (button, action) in wired)
            if (button != null) button.onClick.RemoveListener(action);
        wired.Clear();
    }

    private void Wire(Button button, UnityAction action)
    {
        if (button == null) return;

        button.onClick.AddListener(action);
        wired.Add((button, action));
    }

    private void HandlePageChanged(int page)
    {
        Refresh();
    }

    private void Refresh()
    {
        if (indexBookmark.activeMarker != null) indexBookmark.activeMarker.SetActive(book.IsIndexPage);

        var current = book.CurrentFamily;
        foreach (var bookmark in familyBookmarks)
        {
            if (bookmark.button != null) bookmark.button.interactable = book.HasFamily(bookmark.family);
            if (bookmark.activeMarker != null) bookmark.activeMarker.SetActive(current.HasValue && current.Value == bookmark.family);
        }
    }
}
