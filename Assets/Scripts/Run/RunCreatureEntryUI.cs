using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// One grid cell on the run-summary's page 1: a rarity-specific frame with
/// the creature's icon masked into a circle behind it. The circular masking
/// itself is prefab structure (an Image+Mask pair), NOT runtime code - see
/// this project's setup notes for how to build the prefab.
/// </summary>
public class RunCreatureEntryUI : MonoBehaviour
{
    [Tooltip("The masked creature icon - lives INSIDE the Mask hierarchy in the prefab.")]
    [SerializeField] private Image portrait;
    [Tooltip("The rarity frame graphic - sits OUTSIDE/on top of the Mask hierarchy so its border overlaps the portrait's edge.")]
    [SerializeField] private Image frame;
    [SerializeField] private TMP_Text countText;

    public void Set(CreatureData species, CreatureData.Rarity rarity, int count, Sprite frameSprite)
    {
        if (portrait != null) portrait.sprite = species.GetIcon(rarity);
        if (frame != null) frame.sprite = frameSprite;
        if (countText != null) countText.text = count > 1 ? "x" + count : string.Empty;
    }
}