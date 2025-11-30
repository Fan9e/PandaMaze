using System.Collections;
using UnityEngine;
using UnityEngine.UI;

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
    [SerializeField] private Sprite keySprite;
    [SerializeField] private Sprite potionSprite;
    [SerializeField] private Sprite weaponSprite;

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

    // Populate the three slots with the configured sprites
    private void PopulateSlots()
    {
        SetSlot(0, keySprite);
        SetSlot(1, potionSprite);
        SetSlot(2, weaponSprite);
    }

    private void SetSlot(int index, Sprite sprite)
    {
        if (slotImages == null || index < 0 || index >= slotImages.Length)
            return;

        var img = slotImages[index];
        if (img == null)
            return;

        img.sprite = sprite;
        img.preserveAspect = true;
        img.color = sprite != null ? Color.white : new Color(1f, 1f, 1f, 0.25f);
    }

#if UNITY_EDITOR
    // Keep inspector array size correct while editing
    private void OnValidate()
    {
        if (slotImages == null || slotImages.Length != 3)
            slotImages = new Image[3];
    }
#endif
}
