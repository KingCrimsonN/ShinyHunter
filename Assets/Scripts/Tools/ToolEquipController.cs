using System.Collections;
using UnityEngine;

/// <summary>
/// Reads mouse scroll wheel and number keys (1-9, 0) to change the equipped
/// tool slot, and spawns/despawns the matching ToolBehaviour prefab in the
/// player's hand socket. Left click (configurable) calls UseTool() on
/// whatever's currently held.
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

    private void OnEnable()
    {
        CanUse = true;
        ToolInventoryManager.Instance.OnEquippedChanged += HandleEquippedChanged;
        ToolInventoryManager.Instance.OnInventoryChanged += RefreshHeldToolIfChanged;
        SpawnEquippedTool();
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
        SpawnEquippedTool();
    }

    private void RefreshHeldToolIfChanged()
    {
        // Covers the case where the item sitting in the currently-equipped
        // slot changed (e.g. it was dragged elsewhere, or a stack emptied out).
        var slot = ToolInventoryManager.Instance.EquippedSlot;
        if (slot?.data != currentToolData)
            SpawnEquippedTool();
    }

    private void SpawnEquippedTool()
    {
        if (currentToolInstance != null)
        {
            currentToolInstance.OnUnequip();
            Destroy(currentToolInstance.gameObject);
            currentToolInstance = null;
            handAnimator.Play("Unequip");
        }

        var slot = ToolInventoryManager.Instance.EquippedSlot;
        currentToolData = slot?.data;

        if (currentToolData != null && currentToolData.toolPrefab != null)
        {
            handAnimator.Play("Equip");
            currentToolInstance = Instantiate(currentToolData.toolPrefab, handSocket);
            currentToolInstance.transform.localPosition = Vector3.zero + currentToolInstance.offset;
            currentToolInstance.transform.localRotation = Quaternion.identity;
            currentToolInstance.OnEquip();
        }
    }
}
