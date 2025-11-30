using UnityEngine;

public class Item : MonoBehaviour
{
    // Simple item descriptor usable by chests / pickup logic.
    public enum ItemType
    {
        Key,
        Potion,
        Weapon
    }

    [Tooltip("Category of this item")]
    public ItemType itemType = ItemType.Potion;

    [Tooltip("For Key/Weapon: index (0..2) selecting which variant this is. For Potion this is ignored.")]
    public int variantId = 0;

    [Tooltip("For stackable items like potions, how many to pick up")]
    public int amount = 1;
}
