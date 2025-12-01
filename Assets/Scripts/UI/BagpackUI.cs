using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BagpackUI : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Button that opens/closes the backpack")]
    [SerializeField] private Button bagButton;
    [Tooltip("Parent GameObject that contains the three slot Images")]
    [SerializeField] private GameObject inventoryPanel;
    [Tooltip("Three slot Image components (0 = key, 1 = potion, 2 = weapon)")]
    [SerializeField] private Image[] slotImages = new Image[3];

    [Header("Item Sprites")]
    [Tooltip("Three key variants (indices 0..2)")]
    [SerializeField] private Sprite[] keySprites = new Sprite[3];
    [Tooltip("Potion sprite (single)")]
    [SerializeField] private Sprite potionSprite;
    [Tooltip("Three weapon variants (indices 0..2)")]
    [SerializeField] private Sprite[] weaponSprites = new Sprite[3];

    [Header("Empty Slot / Placeholder")]
    [Tooltip("Show a placeholder sprite when the slot is empty")]
    [SerializeField] private bool showEmptyPlaceholders = true;
    [Tooltip("Optional sprite to display for empty slots (will be shown with reduced alpha)")]
    [SerializeField] private Sprite emptySlotSprite;

    [Header("Runtime / Inspector Controls")]
    [Tooltip("Selected key variant (0..2)")]
    [SerializeField, Range(0, 2)] private int keyVariant = 0;
    [Tooltip("Whether the player currently has a key")]
    [SerializeField] private bool hasKey = false;
    [Tooltip("Selected weapon variant (0..2)")]
    [SerializeField, Range(0, 2)] private int weaponVariant = 0;
    [Tooltip("Whether the player currently has a weapon")]
    [SerializeField] private bool hasWeapon = false;
    [Tooltip("Number of potions held")]
    [SerializeField, Min(0)] private int potionCount = 0;

    [Header("Optional UI")]
    [Tooltip("Optional Text element to display potion count")]
    [SerializeField] private TMP_Text potionCountText;

    private bool isOpen;

    private void Awake()
    {
        // Ensure panel starts closed
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
        // Key slot (index 0)
        Sprite key = null;
        if (hasKey)
            key = (keySprites != null && keySprites.Length > keyVariant) ? keySprites[keyVariant] : null;
        else if (showEmptyPlaceholders)
            key = emptySlotSprite;
        SetSlot(0, key, hasKey);

        // Potion slot (index 1)
        Sprite potion = null;
        bool potionVisible = potionCount > 0;
        if (potionVisible)
            potion = potionSprite;
        else if (showEmptyPlaceholders)
            potion = emptySlotSprite;
        SetSlot(1, potion, potionVisible);

        // Weapon slot (index 2)
        Sprite weapon = null;
        if (hasWeapon)
            weapon = (weaponSprites != null && weaponSprites.Length > weaponVariant) ? weaponSprites[weaponVariant] : null;
        else if (showEmptyPlaceholders)
            weapon = emptySlotSprite;
        SetSlot(2, weapon, hasWeapon);

        UpdatePotionText();
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

        // Determine color: full white when owned item, reduced alpha when placeholder or empty
        if (sprite == null)
        {
            img.color = new Color(1f, 1f, 1f, 0.25f);
        }
        else
        {
            // If sprite is the empty placeholder, show it with reduced opacity
            if (showEmptyPlaceholders && emptySlotSprite != null && sprite == emptySlotSprite && !owned)
                img.color = new Color(1f, 1f, 1f, 0.5f);
            else if (!owned)
                img.color = new Color(1f, 1f, 1f, 0.5f);
            else
                img.color = Color.white;
        }
    }

    private void UpdatePotionText()
    {
        if (potionCountText != null)
        {
            if (potionCount > 1)
                potionCountText.text = potionCount.ToString();
            else if (potionCount == 1)
                potionCountText.text = "1";
            else
                potionCountText.text = ""; // hide when zero
        }
    }

    // Public API usable from editor or runtime

    public void SetKeyVariant(int variant)
    {
        keyVariant = Mathf.Clamp(variant, 0, 2);
        PopulateSlots();
    }

    public void SetWeaponVariant(int variant)
    {
        weaponVariant = Mathf.Clamp(variant, 0, 2);
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
        if (slotImages == null || slotImages.Length != 3)
            slotImages = new Image[3];

        if (keySprites == null || keySprites.Length != 3)
            keySprites = new Sprite[3];

        if (weaponSprites == null || weaponSprites.Length != 3)
            weaponSprites = new Sprite[3];

        keyVariant = Mathf.Clamp(keyVariant, 0, 2);
        weaponVariant = Mathf.Clamp(weaponVariant, 0, 2);
        potionCount = Mathf.Max(0, potionCount);

        // In editor, reflect changes immediately
        if (!Application.isPlaying)
            PopulateSlots();
    }
#endif
}
