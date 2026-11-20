using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The "you caught it!" toast (Overlay's CapturePopup): the critter's icon in
/// its rarity frame, its name with the rarity ("Shining Appy"), a NEW badge
/// the first time that species is caught AT THAT RARITY (a Normal Appy
/// already caught doesn't stop a Shining Appy from being new), and the
/// double-ingredient badge when the capture rolled it.
///
/// Snaps in from the left edge of the screen (overshooting ease), holds, then
/// slides back out. Captures that arrive while one is showing queue up and play
/// one after another. Unscaled time, so it isn't stuck if the tablet pauses.
///
/// Put it ON the CapturePopup object, which must be ACTIVE in the prefab
/// (it hides itself in Awake). Listens to the static
/// InventoryManager.OnCritterCaptured, subscribed Awake/OnDestroy rather than
/// OnEnable/OnDisable because this object deactivates itself between toasts;
/// the event is static, so it doesn't matter which persistent manager wakes
/// first, and duplicate Overlays unsubscribe again when they are destroyed.
/// </summary>
public class CapturePopupUI : MonoBehaviour
{
    [Header("Parts")]
    [Tooltip("What slides. Empty = this object.")]
    [SerializeField] private RectTransform panel;
    [SerializeField] private Image icon;
    [SerializeField] private Image frame;
    [SerializeField] private TMP_Text nameText;
    [Tooltip("Shown only the first time this species is caught at this rarity.")]
    [SerializeField] private GameObject newBadge;
    [Tooltip("Optional - shown when the capture rolled double ingredients.")]
    [SerializeField] private GameObject doubleYieldBadge;
    [Tooltip("Rarity frames (rarityFrames) - the same asset the inventory uses.")]
    [SerializeField] private CritterFrames critterFrames;

    [Header("Name - index = rarity: Normal, Uncommon, Rare, Legendary. {0} = the critter's name")]
    [SerializeField] private string[] nameFormats = { "{0}", "Sparkling {0}", "Shining {0}", "Radiant {0}" };

    [Header("Animation")]
    [SerializeField] private float slideInDuration = 0.3f;
    [SerializeField] private Ease slideInEase = Ease.OutBack;
    [Tooltip("Seconds it stays on screen.")]
    [SerializeField] private float holdDuration = 2.5f;
    [SerializeField] private float slideOutDuration = 0.25f;
    [SerializeField] private Ease slideOutEase = Ease.InBack;
    [Tooltip("Extra pixels past the left screen edge it starts/ends at.")]
    [SerializeField] private float offscreenMargin = 50f;
    [Tooltip("Punch on the NEW badge once the popup has arrived. 0 = none.")]
    [SerializeField] private float newBadgePunch = 0.35f;

    private readonly Queue<InventoryManager.CaptureInfo> queue = new Queue<InventoryManager.CaptureInfo>();
    private Vector2 shownPosition;
    private Sequence sequence;

    private void Awake()
    {
        if (panel == null) panel = (RectTransform)transform;
        shownPosition = panel.anchoredPosition; // where it's placed in the prefab = where it lands

        InventoryManager.OnCritterCaptured += Enqueue;
        panel.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        InventoryManager.OnCritterCaptured -= Enqueue;
        sequence?.Kill();
    }

    private void Enqueue(InventoryManager.CaptureInfo info)
    {
        queue.Enqueue(info);
        if (sequence == null || !sequence.IsActive()) ShowNext();
    }

    private void ShowNext()
    {
        sequence = null;
        if (queue.Count == 0)
        {
            panel.gameObject.SetActive(false);
            return;
        }

        Fill(queue.Dequeue());

        panel.gameObject.SetActive(true);
        panel.anchoredPosition = shownPosition;
        LayoutRebuilder.ForceRebuildLayoutImmediate(panel); // the name changed - let the layout group resize before measuring
        Vector2 hiddenPosition = shownPosition - new Vector2(OffscreenDistance(), 0f);
        panel.anchoredPosition = hiddenPosition;

        sequence = DOTween.Sequence()
            .Append(panel.DOAnchorPos(shownPosition, slideInDuration).SetEase(slideInEase));
        if (newBadge != null && newBadge.activeSelf && newBadgePunch > 0f)
            sequence.Append(newBadge.transform.DOPunchScale(Vector3.one * newBadgePunch, 0.3f, 6, 0.5f));
        sequence
            .AppendInterval(holdDuration)
            .Append(panel.DOAnchorPos(hiddenPosition, slideOutDuration).SetEase(slideOutEase))
            .SetUpdate(true)
            .SetLink(gameObject)
            .OnComplete(ShowNext);
    }

    private void Fill(InventoryManager.CaptureInfo info)
    {
        var species = info.species;
        int rarity = (int)info.rarity;

        if (icon != null) icon.sprite = species.GetIcon(info.rarity);
        if (frame != null && critterFrames != null) frame.sprite = CritterFrames.Pick(critterFrames.rarityFrames, rarity, frame.sprite);
        if (nameText != null) nameText.text = string.Format(CritterFrames.Pick(nameFormats, rarity, "{0}"), species.creatureName);
        if (newBadge != null)
        {
            newBadge.SetActive(info.firstOfRarity);
            newBadge.transform.localScale = Vector3.one;
        }
        if (doubleYieldBadge != null) doubleYieldBadge.SetActive(info.doubleYield);
    }

    /// <summary>How far left (in the panel's own units) it must move so its right edge is past the screen's left edge.</summary>
    private float OffscreenDistance()
    {
        var canvas = panel.GetComponentInParent<Canvas>();
        Camera cam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
        float scale = canvas != null && canvas.rootCanvas.scaleFactor > 0f ? canvas.rootCanvas.scaleFactor : 1f;

        var corners = new Vector3[4];
        panel.GetWorldCorners(corners);
        float rightEdge = RectTransformUtility.WorldToScreenPoint(cam, corners[2]).x; // corners[2] = top right

        return Mathf.Max(0f, rightEdge) / scale + offscreenMargin;
    }
}
