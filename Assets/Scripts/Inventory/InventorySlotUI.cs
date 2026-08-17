using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>One row in the inventory list: icon, name, count.</summary>
public class InventorySlotUI : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text countText;

    public void Set(CreatureData data, int count)
    {
        if (icon != null) icon.sprite = data.icon;
        if (nameText != null) nameText.text = data.creatureName;
        if (countText != null) countText.text = "x" + count;
    }
}
