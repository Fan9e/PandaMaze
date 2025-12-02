using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Håndterer visning og interaktion for spillerens rygsæk/inventory UI.
/// Indeholder tre faste slots: nøgle, potion og våben. UI opdateres både i editor og runtime.
/// </summary>
public class BagpackUI : MonoBehaviour
{
    /// <summary>Antal slots i inventory (fast = 3).</summary>
    private const int SlotCount = 3;
    /// <summary>Antal varianter per item-type hvor relevant (fast = 3).</summary>
    private const int VariantCount = 3;

    /// <summary>Indekser for de tre faste slot-typer.</summary>
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

    /// <summary>Om inventory-panelet aktuelt er åbent.</summary>
    private bool isOpen;

    /// <summary>Farve der bruges når et item ejes (fuld alpha).</summary>
    private static readonly Color OwnedColor = Color.white;
    /// <summary>Alpha værdi der bruges til placeholder-ikoner.</summary>
    private const float PlaceholderAlpha = 0.5f;
    /// <summary>Alpha værdi der bruges for helt tomme slots.</summary>
        private const float EmptySlotAlpha = 0.25f;

    /// <summary>
    /// Unity Awake: initialiserer UI (skjuler panel, binder knap og udfylder slots).
    /// Kører før Start.
    /// </summary>
    private void Awake()
    {
        if (inventoryPanel != null)
            inventoryPanel.SetActive(false);

        if (bagButton != null)
            bagButton.onClick.AddListener(ToggleInventory);

        PopulateSlots();
    }

    /// <summary>
    /// Fjern event-listeners ved ødelæggelse af objektet for at undgå memory leaks.
    /// </summary>
    private void OnDestroy()
    {
        if (bagButton != null)
            bagButton.onClick.RemoveListener(ToggleInventory);
    }

    /// <summary>
    /// Skifter mellem at åbne og lukke inventory-panelet.
    /// </summary>
    public void ToggleInventory()
    {
        isOpen = !isOpen;
        if (inventoryPanel != null)
            inventoryPanel.SetActive(isOpen);
    }

    /// <summary>
    /// Opdaterer alle slots ud fra aktuelle ejerskaber og varianter.
    /// Sætter også potion-teksten.
    /// </summary>
    private void PopulateSlots()
    {
        SetSlot((int)SlotType.Key, GetSpriteForKey(), hasKey);
        SetSlot((int)SlotType.Potion, GetSpriteForPotion(), potionCount > 0);
        SetSlot((int)SlotType.Weapon, GetSpriteForWeapon(), hasWeapon);

        UpdatePotionText();
    }

    /// <summary>
    /// Returnerer korrekt sprite for nøgle baseret på variant og ejerskab.
    /// Hvis ingen nøgle men placeholders er aktiveret, returneres <see cref="emptySlotSprite"/>.
    /// </summary>
    /// <returns>Sprite til nøgle-slot eller null/placeholder.</returns>
    private Sprite GetSpriteForKey()
    {
        if (hasKey && keySprites != null && keySprites.Length > keyVariant)
            return keySprites[keyVariant];

        return showEmptyPlaceholders ? emptySlotSprite : null;
    }

    /// <summary>
    /// Returnerer potion-sprite hvis der er potions; ellers placeholder eller null.
    /// </summary>
    /// <returns>Sprite til potion-slot eller null/placeholder.</returns>
    private Sprite GetSpriteForPotion()
    {
        if (potionCount > 0 && potionSprite != null)
            return potionSprite;

        return showEmptyPlaceholders ? emptySlotSprite : null;
    }

    /// <summary>
    /// Returnerer korrekt sprite for våben baseret på variant og ejerskab.
    /// Hvis ingen våben men placeholders er aktiveret, returneres <see cref="emptySlotSprite"/>.
    /// </summary>
    /// <returns>Sprite til våben-slot eller null/placeholder.</returns>
    private Sprite GetSpriteForWeapon()
    {
        if (hasWeapon && weaponSprites != null && weaponSprites.Length > weaponVariant)
            return weaponSprites[weaponVariant];

        return showEmptyPlaceholders ? emptySlotSprite : null;
    }

    /// <summary>
    /// Sætter et enkelt slot-image (sprite og alpha) baseret på index og ejerskab.
    /// Håndterer også placeholder- og tomme-tilstande.
    /// </summary>
    /// <param name="index">Index i `slotImages` for det slot der skal sættes.</param>
    /// <param name="sprite">Sprite der skal vises (kan være null eller placeholder).</param>
    /// <param name="owned">Om spilleren ejer itemet (bruges til alpha-beregning).</param>
    private void SetSlot(int index, Sprite sprite, bool owned = true)
    {
        if (slotImages == null || index < 0 || index >= slotImages.Length)
            return;

        var img = slotImages[index];
        if (img == null)
            return;

        img.sprite = sprite;
        img.preserveAspect = true;

        if (sprite == null)
        {
            img.color = new Color(1f, 1f, 1f, EmptySlotAlpha);
            return;
        }

        bool isPlaceholder = showEmptyPlaceholders && emptySlotSprite != null && sprite == emptySlotSprite;
        if (!owned || isPlaceholder)
        {
            img.color = new Color(1f, 1f, 1f, PlaceholderAlpha);
            return;
        }

        img.color = OwnedColor;
    }

    /// <summary>
    /// Opdaterer valgfri potion-count tekst i UI.
    /// Viser int-antal eller tom streng hvis 0.
    /// </summary>
    private void UpdatePotionText()
    {
        if (potionCountText == null) return;

        potionCountText.text = potionCount > 0 ? potionCount.ToString() : string.Empty;
    }

    /// <summary>
    /// Sætter aktiv key-variant (clampet til gyldigt interval) og opdaterer UI.
    /// </summary>
    /// <param name="variant">Variantindex (0..VariantCount-1).</param>
    public void SetKeyVariant(int variant)
    {
        keyVariant = Mathf.Clamp(variant, 0, VariantCount - 1);
        PopulateSlots();
    }

    /// <summary>
    /// Sætter aktiv weapon-variant (clampet til gyldigt interval) og opdaterer UI.
    /// </summary>
    /// <param name="variant">Variantindex (0..VariantCount-1).</param>
    public void SetWeaponVariant(int variant)
    {
        weaponVariant = Mathf.Clamp(variant, 0, VariantCount - 1);
        PopulateSlots();
    }

    /// <summary>
    /// Tilføjer potions (positive amount). Ignorerer negative værdier.
    /// Opdaterer UI.
    /// </summary>
    /// <param name="amount">Antal der skal lægges til (standard 1).</param>
    public void AddPotions(int amount = 1)
    {
        potionCount = Mathf.Max(0, potionCount + Mathf.Max(0, amount));
        PopulateSlots();
    }

    /// <summary>
    /// Fjerner potions (positive amount). Ignorerer negative værdier.
    /// Opdaterer UI.
    /// </summary>
    /// <param name="amount">Antal der skal fjernes (standard 1).</param>
    public void RemovePotions(int amount = 1)
    {
        potionCount = Mathf.Max(0, potionCount - Mathf.Max(0, amount));
        PopulateSlots();
    }

    /// <summary>
    /// Sætter potion-antal direkte (clampet til >= 0) og opdaterer UI.
    /// </summary>
    /// <param name="amount">Nyt antal potions.</param>
    public void SetPotions(int amount)
    {
        potionCount = Mathf.Max(0, amount);
        PopulateSlots();
    }

    /// <summary>
    /// Sætter om spilleren har en nøgle og opdaterer UI.
    /// </summary>
    /// <param name="owned">True hvis nøglen ejes.</param>
    public void SetHasKey(bool owned)
    {
        hasKey = owned;
        PopulateSlots();
    }

    /// <summary>
    /// Sætter om spilleren har et våben og opdaterer UI.
    /// </summary>
    /// <param name="owned">True hvis våbenet ejes.</param>
    public void SetHasWeapon(bool owned)
    {
        hasWeapon = owned;
        PopulateSlots();
    }

#if UNITY_EDITOR

    /// <summary>
    /// Editor-time validering: sørger for korrekte array-størrelser og gyldige værdier.
    /// Kører i editor når værdier ændres i Inspector.
    /// </summary>
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

        PopulateSlots();
    }
#endif
}
