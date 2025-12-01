using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BagpackUI : MonoBehaviour
{
    private const int SlotCount = 3;
    private const int VariantCount = 3;

    private enum SlotType { Key = 0, Potion = 1, Weapon = 2 }

    [Header("UI References")]
    [Tooltip("Button that opens/closes the backpack")]
    [SerializeField] private Button bagButton;
    [Tooltip("Parent GameObject that contains the three slot Images")]
    [SerializeField] private GameObject inventoryPanel;
    [Tooltip("Three slot Image components (0 = key, 1 = potion, 2 = weapon)")]
    [SerializeField] private Image[] slotImages = new Image[SlotCount];

    [Header("Item Sprites")]
    [Tooltip("Three key variants (indices 0..2)")]
    [SerializeField] private Sprite[] keySprites = new Sprite[VariantCount];
    [Tooltip("Potion sprite (single)")]
    [SerializeField] private Sprite potionSprite;
    [Tooltip("Three weapon variants (indices 0..2)")]
    [SerializeField] private Sprite[] weaponSprites = new Sprite[VariantCount];

    [Header("Empty Slot / Placeholder")]
    [Tooltip("Show a placeholder sprite when the slot is empty")]
    [SerializeField] private bool showEmptyPlaceholders = true;
    [Tooltip("Optional sprite to display for empty slots (will be shown with reduced alpha)")]
    [SerializeField] private Sprite emptySlotSprite;

    [Header("Runtime / Inspector Controls")]
    [Tooltip("Selected key variant (0..2)")]
    [SerializeField, Range(0, VariantCount - 1)] private int keyVariant = 0;
    [Tooltip("Whether the player currently has a key")]
    [SerializeField] private bool hasKey = false;
    [Tooltip("Selected weapon variant (0..2)")]
    [SerializeField, Range(0, VariantCount - 1)] private int weaponVariant = 0;
    [Tooltip("Whether the player currently has a weapon")]
    [SerializeField] private bool hasWeapon = false;
    [Tooltip("Number of potions held")]
    [SerializeField, Min(0)] private int potionCount = 0;

    [Header("Optional UI")]
    [Tooltip("Optional Text element to display potion count")]
    [SerializeField] private TMP_Text potionCountText;

    private bool isOpen;

    // Colors/alphas used by the UI
    private static readonly Color OwnedColor = Color.white;
    private const float PlaceholderAlpha = 0.5f;
    private const float EmptySlotAlpha = 0.25f;

    private void Awake()
    {
        if (inventoryPanel != null)
            inventoryPanel.SetActive(false);

        if (bagButton != null)
            bagButton.onClick.AddListener(ToggleInventory);

        PopulateSlots();
    }

    private void OnDestroy()
    {
        if (bagButton != null)
            bagButton.onClick.RemoveListener(ToggleInventory);
    }

    // Toggle open/closed state
    public void ToggleInventory()
    {
        isOpen = !isOpen;
        if (inventoryPanel != null)
            inventoryPanel.SetActive(isOpen);
    }

    // Populate the three slots with the configured sprites or placeholders when empty
    private void PopulateSlots()
    {
        SetSlot((int)SlotType.Key, GetSpriteForKey(), hasKey);
        SetSlot((int)SlotType.Potion, GetSpriteForPotion(), potionCount > 0);
        SetSlot((int)SlotType.Weapon, GetSpriteForWeapon(), hasWeapon);

        UpdatePotionText();
    }

    private Sprite GetSpriteForKey()
    {
        if (hasKey && keySprites != null && keySprites.Length > keyVariant)
            return keySprites[keyVariant];

        return showEmptyPlaceholders ? emptySlotSprite : null;
    }

    private Sprite GetSpriteForPotion()
    {
        if (potionCount > 0 && potionSprite != null)
            return potionSprite;

        return showEmptyPlaceholders ? emptySlotSprite : null;
    }

    private Sprite GetSpriteForWeapon()
    {
        if (hasWeapon && weaponSprites != null && weaponSprites.Length > weaponVariant)
            return weaponSprites[weaponVariant];

        return showEmptyPlaceholders ? emptySlotSprite : null;
    }

    // index: slot index, sprite: sprite to assign (can be placeholder), owned: true when the player actually has the item
    private void SetSlot(int index, Sprite sprite, bool owned = true)
    {
        if (slotImages == null || index < 0 || index >= slotImages.Length)
            return;

        var img = slotImages[index];
        if (img == null)
            return;

        img.sprite = sprite;
        img.preserveAspect = true;

        // Null sprite -> faint/hidden
        if (sprite == null)
        {
            img.color = new Color(1f, 1f, 1f, EmptySlotAlpha);
            return;
        }

        // If this is an explicit empty placeholder (and placeholders are enabled) or not owned -> semi-transparent
        bool isPlaceholder = showEmptyPlaceholders && emptySlotSprite != null && sprite == emptySlotSprite;
        if (!owned || isPlaceholder)
        {
            img.color = new Color(1f, 1f, 1f, PlaceholderAlpha);
            return;
        }

        img.color = OwnedColor;
    }

    private void UpdatePotionText()
    {
        if (potionCountText == null) return;

        // Show count when more than zero; empty string hides the text visually.
        potionCountText.text = potionCount > 0 ? potionCount.ToString() : string.Empty;
    }

    // Public API usable from editor or runtime

    public void SetKeyVariant(int variant)
    {
        keyVariant = Mathf.Clamp(variant, 0, VariantCount - 1);
        PopulateSlots();
    }

    public void SetWeaponVariant(int variant)
    {
        weaponVariant = Mathf.Clamp(variant, 0, VariantCount - 1);
        PopulateSlots();
    }

    public void AddPotions(int amount = 1)
    {
        potionCount = Mathf.Max(0, potionCount + Mathf.Max(0, amount));
        PopulateSlots();
    }

    public void RemovePotions(int amount = 1)
    {
        potionCount = Mathf.Max(0, potionCount - Mathf.Max(0, amount));
        PopulateSlots();
    }

    public void SetPotions(int amount)
    {
        potionCount = Mathf.Max(0, amount);
        PopulateSlots();
    }

    public void SetHasKey(bool owned)
    {
        hasKey = owned;
        PopulateSlots();
    }

    public void SetHasWeapon(bool owned)
    {
        hasWeapon = owned;
        PopulateSlots();
    }

#if UNITY_EDITOR
    // Keep inspector array size correct while editing
    private void OnValidate()
    {
        if (slotImages == null || slotImages.Length != SlotCount)
            slotImages = new Image[SlotCount];

        if (keySprites == null || keySprites.Length != VariantCount)
            keySprites = new Sprite[VariantCount];

        if (weaponSprites == null || weaponSprites.Length != VariantCount)
            weaponSprites = new Sprite[VariantCount];

        keyVariant = Mathf.Clamp(keyVariant, 0, VariantCount - 1);
        weaponVariant = Mathf.Clamp(weaponVariant, 0, VariantCount - 1);
        potionCount = Mathf.Max(0, potionCount);

        // Reflect changes immediately in the Inspector
        PopulateSlots();
    }
#endif
}
