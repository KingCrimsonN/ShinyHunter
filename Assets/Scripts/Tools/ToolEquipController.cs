using System.Collections;
using UnityEngine;

/// <summary>
/// Reads mouse scroll wheel and number keys (1-9, 0) to change the equipped
/// tool slot, and spawns/despawns the matching ToolBehaviour prefab in the
/// player's hand socket. Left click (configurable) calls UseTool() on
/// whatever's currently held.
///
/// Tool swapping is a coroutine-driven sequence (Unequip -> destroy old ->
/// instantiate new -> Equip -> Idle) rather than firing both Animator states
/// in the same frame, and each new swap request cancels any swap already in
/// progress. This is what prevents the animator getting stuck mid-blend when
/// the player switches tools quickly - see SwapToolRoutine's comments.
/// </summary>
public class ToolEquipController : MonoBehaviour
{
    [Tooltip("Empty transform (child of camera, positioned where held items should appear) that spawned tool prefabs are parented to.")]
    [SerializeField] private Transform handSocket;
    [SerializeField] private GameObject handSocketVisual;
    [SerializeField] private Animator handAnimator;
    [SerializeField] private KeyCode useKey = KeyCode.Mouse0;

    public bool CanUse;

    private ToolBehaviour currentToolInstance;
    private ToolData currentToolData;
    private Coroutine swapRoutine;

    private void OnEnable()
    {
        CanUse = true;
        ToolInventoryManager.Instance.OnEquippedChanged += HandleEquippedChanged;
        ToolInventoryManager.Instance.OnInventoryChanged += RefreshHeldToolIfChanged;
        RequestSwap();
    }

    private void OnDisable()
    {
        if (ToolInventoryManager.Instance != null)
        {
            ToolInventoryManager.Instance.OnEquippedChanged -= HandleEquippedChanged;
            ToolInventoryManager.Instance.OnInventoryChanged -= RefreshHeldToolIfChanged;
        }
    }

    public void EnableUse()
    {
        StartCoroutine(UseCooldownCoroutine(0.1f));
    }

    private IEnumerator UseCooldownCoroutine(float cooldown)
    {
        CanUse = false;
        yield return new WaitForSeconds(cooldown);
        CanUse = true;
    }

    private void Update()
    {
        if (!CanUse) return;
        UpdateTool();
        HandleScrollInput();
        HandleNumberKeyInput();

        if (Input.GetKeyDown(useKey))
            currentToolInstance?.UseTool();
    }

    private void UpdateTool()
    {
        currentToolInstance?.Update();
    }

    private void HandleScrollInput()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll > 0f) ToolInventoryManager.Instance.CycleEquipped(-1);
        else if (scroll < 0f) ToolInventoryManager.Instance.CycleEquipped(1);
    }

    private void HandleNumberKeyInput()
    {
        // Keys 1-9 map to slots 0-8
        for (int i = 0; i < 9; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                ToolInventoryManager.Instance.SetEquippedIndex(i);
                return;
            }
        }

        // Key 0 maps to slot 9 (the 10th slot)
        if (Input.GetKeyDown(KeyCode.Alpha0))
            ToolInventoryManager.Instance.SetEquippedIndex(9);
    }

    private void HandleEquippedChanged(int index)
    {
        RequestSwap();
    }

    private void RefreshHeldToolIfChanged()
    {
        // Covers the case where the item sitting in the currently-equipped
        // slot changed (e.g. it was dragged elsewhere, or a stack emptied out).
        var slot = ToolInventoryManager.Instance.EquippedSlot;
        if (slot?.data != currentToolData)
            RequestSwap();
    }

    /// <summary>
    /// Entry point for any tool change. Cancels whatever swap sequence is
    /// currently running (if any) and starts a fresh one - this is what
    /// makes rapid switching converge cleanly onto the last-selected tool
    /// instead of stacking overlapping Play() calls.
    /// </summary>
    private void RequestSwap()
    {
        if (swapRoutine != null) StopCoroutine(swapRoutine);
        swapRoutine = StartCoroutine(SwapToolRoutine());
    }

    private IEnumerator SwapToolRoutine()
    {
        // --- Unequip whatever's currently held, and WAIT for that animation
        // to actually finish before touching the tool GameObject. Explicit
        // (0, 0f) forces a clean restart from frame 0 even if "Unequip" was
        // already playing (e.g. this routine was cancelled and restarted
        // mid-unequip) - a bare Play("Unequip") can silently no-op if the
        // Animator thinks that state is already active.
        if (currentToolInstance != null)
        {
            currentToolInstance.OnUnequip(); // note: if the player switches again before this finishes, RequestSwap cancels and restarts this routine, so OnUnequip can fire more than once on the same instance - keep it idempotent in your ToolBehaviour subclasses
            if (handAnimator != null)
            {
                handAnimator.Play("Unequip", 0, 0f);
                yield return null; // let the Animator register the new current state before we read its length
                yield return new WaitForSeconds(handAnimator.GetCurrentAnimatorStateInfo(0).length);
            }

            Destroy(currentToolInstance.gameObject);
            currentToolInstance = null;
        }

        // Re-fetch here rather than at the top of the routine, so if the
        // player scrolled past several tools while we were mid-unequip we
        // end up on whichever one is ACTUALLY selected now, not a stale one.
        var slot = ToolInventoryManager.Instance.EquippedSlot;
        currentToolData = slot?.data;

        if (currentToolData != null && currentToolData.toolPrefab != null)
        {
            currentToolInstance = Instantiate(currentToolData.toolPrefab, handSocket);
            currentToolInstance.transform.localPosition = Vector3.zero + currentToolInstance.offset;
            currentToolInstance.transform.localRotation = Quaternion.identity;
            currentToolInstance.OnEquip();

            if (handAnimator != null)
            {
                handAnimator.Play("Equip", 0, 0f);
                yield return null;
                yield return new WaitForSeconds(handAnimator.GetCurrentAnimatorStateInfo(0).length);
                handAnimator.Play("Idle", 0, 0f);
            }
        }
        else if (handAnimator != null)
        {
            handAnimator.Play("Empty", 0, 0f);
        }

        swapRoutine = null;
    }
}