using DG.Tweening;
using UnityEngine;

/// <summary>
/// A notification-style "new" icon outside the dex itself - e.g. on the
/// tablet's Notes tab. Shows Badge while anything in the CritterDex is new
/// (caught for the first time at that rarity, not looked at yet).
///
/// Put it on an object that stays active (the tab button), with Badge a child
/// that starts hidden. Safe on the persistent Overlay: subscribes in
/// OnEnable/OnDisable to a static event, holds no scene references.
/// </summary>
public class CritterDexNewIndicator : MonoBehaviour
{
    [Tooltip("The icon to show/hide. Its own object, not this one.")]
    [SerializeField] private GameObject badge;
    [Tooltip("Pop when it appears. 0 = none.")]
    [SerializeField] private float appearPunch = 0.3f;

    private void OnEnable()
    {
        InventoryManager.OnDexNewChanged += Refresh;
        Refresh(false);
    }

    private void OnDisable()
    {
        InventoryManager.OnDexNewChanged -= Refresh;
    }

    private void Refresh() => Refresh(true);

    private void Refresh(bool animate)
    {
        if (badge == null) return;

        bool show = InventoryManager.Instance != null && InventoryManager.Instance.HasAnyNewInDex;
        bool appearing = show && !badge.activeSelf;
        badge.SetActive(show);

        if (appearing && animate && appearPunch > 0f)
        {
            var t = badge.transform;
            t.DOKill(true);
            t.DOPunchScale(Vector3.one * appearPunch, 0.35f, 6, 0.5f).SetUpdate(true).SetLink(badge);
        }
    }
}
