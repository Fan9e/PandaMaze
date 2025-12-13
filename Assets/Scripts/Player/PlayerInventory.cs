using System.Collections.Generic;
using UnityEngine;


public class PlayerInventory : MonoBehaviour
{
    [SerializeField] private PlayerWeapon playerWeapon;

    /// <summary>
    /// Reference til PlayerWeapon på spilleren (kan sættes i Inspector eller findes automatisk).
    /// </summary>
    public PlayerWeapon PlayerWeapon => playerWeapon;
    /// <summary>Reference til UI’en, så vi kan opdatere nøgle-slot.</summary>
    [SerializeField] private BagpackUI bagpackUI;

    /// <summary>
    /// Mængde af nøgler spilleren har samlet op.
    /// HashSet sikrer, at der ikke kan opstå dubletter.
    /// </summary>
    [SerializeField] private readonly HashSet<int> _collectedKeys = new HashSet<int>();

    [SerializeField] private List<int> _debugKeys = new List<int>();
    [Header("Potions")]
    [SerializeField, Min(0)] private int potionCount = 0;
    private void Awake()
    {
        InitializeBackpackUI();
        InitializePlayerWeapon();
        UpdateKeyUIAfterChange();
        UpdatePotionUI();
    }
    private void InitializeBackpackUI()
    {
        if (bagpackUI == null)
        {
            bagpackUI = FindObjectOfType<BagpackUI>();
            if (bagpackUI == null)
            {
                Debug.LogWarning("PlayerInventory kunne ikke finde nogen BagpackUI i scenen.", this);
            }
        }
    }
    private void InitializePlayerWeapon()
    {
        if (playerWeapon == null)
            TryGetComponent(out playerWeapon);

        if (playerWeapon == null)
            Debug.LogWarning("PlayerInventory kunne ikke finde PlayerWeapon på spilleren.", this);
    }
    /// <summary>
    /// Tilføjer en nøgle til spillerens inventory.
    /// Returnerer true hvis nøglen blev tilføjet, og false hvis spilleren allerede havde den.
    /// </summary>
    /// <param name="keyId">ID på nøglen der skal tilføjes.</param>
    public bool AddKey(int keyId)
    {
        if (_collectedKeys.Contains(keyId))
            return false;

        _collectedKeys.Add(keyId);
        UpdateKeyUIAfterChange(keyId);
        _debugKeys.Clear();
        _debugKeys.AddRange(_collectedKeys);  // kun for at vise dem i Inspector
        Debug.Log($"Spilleren har samlet nøgle {keyId}");
        return true;
    }

    /// <summary>
    /// Tjekker om spilleren har en bestemt nøgle.
    /// Bruges af døre for at afgøre, om de må åbnes.
    /// </summary>
    /// <param name="keyId">ID på den nøgle, der skal tjekkes.</param>
    /// <returns>True hvis spilleren har nøglen, ellers false.</returns>
    public bool HasKey(int keyId)
    {
        return _collectedKeys.Contains(keyId);
    }
    /// <summary>
    /// Fjerner en nøgle (hvis du fx vil "bruge" den til en dør)
    /// og opdaterer UI derefter.
    /// </summary>
    public bool RemoveKey(int keyId)
    {
        if (!_collectedKeys.Remove(keyId))
            return false;

        Debug.Log($"Spilleren har brugt/fjernet nøgle {keyId}");

        UpdateKeyUIAfterChange();

        return true;
    }

    /// <summary>
    /// Opdaterer BagpackUI baseret på de nøgler, spilleren har.
    /// Hvis der ikke er nogen nøgler, skjules ikonet.
    /// Hvis der er mindst én, viser vi varianten ud fra en valgt keyId.
    /// </summary>
    private void UpdateKeyUIAfterChange(int lastAddedKeyId = -1)
    {
        if (bagpackUI == null)
            return;

        if (_collectedKeys.Count == 0)
        {
            // Ingen nøgler → slot “tomt”
            bagpackUI.SetHasKey(false);
            return;
        }

        // Vælg hvilken nøgle der skal styre varianten:
        int keyIdToUse = lastAddedKeyId;

        // Hvis vi ikke fik en specifik (fx RemoveKey), tag bare en vilkårlig
        if (keyIdToUse < 0)
        {
            foreach (int id in _collectedKeys)
            {
                keyIdToUse = id;
                break;
            }
        }

        // Map keyId -> variant-index (0..2)
        int variant = MapKeyIdToVariant(keyIdToUse);

        bagpackUI.SetHasKey(true);
        bagpackUI.SetKeyVariant(variant);
    }

    /// <summary>
    /// Her bestemmer du, hvordan keyId oversættes til sprite-varianten (0..2)
    /// i BagpackUI.keySprites.
    /// </summary>
    private int MapKeyIdToVariant(int keyId)
    {
        
        const int VariantCount = 3; // samme som i BagpackUI
        int variant = Mathf.Clamp(keyId - 1, 0, VariantCount - 1);
        return variant;
    }

    /// <summary>
    /// Tilføjer potions til spilleren og opdaterer BagpackUI.
    /// </summary>
    public void AddPotions(int amount)
    {
        if (amount <= 0) return;

        potionCount += amount;
        UpdatePotionUI();
        Debug.Log($"Spilleren har fået {amount} potion(s). Total: {potionCount}");
    }

    /// <summary>
    /// Forsøger at bruge én potion. Returnerer true hvis det lykkedes.
    /// </summary>
    public bool TryConsumePotion()
    {
        if (potionCount <= 0)
            return false;

        potionCount--;
        UpdatePotionUI();
        Debug.Log($"Spilleren brugte en potion. Tilbage: {potionCount}");
        return true;
    }

    /// <summary>
    /// Læser nuværende antal potions.
    /// </summary>
    public int GetPotionCount() => potionCount;

    /// <summary>
    /// Opdaterer BagpackUI’s potion-slot og tekst via det eksisterende BagpackUI-API.
    /// (Vi ændrer ikke BagpackUI-scriptet, vi bruger bare SetPotions).
    /// </summary>
    private void UpdatePotionUI()
    {
        if (bagpackUI == null)
            return;

        bagpackUI.SetPotions(potionCount);
    }

    public void EquipWeapon(Weapon weaponPrefab)
    {
        if (playerWeapon == null) return;
        playerWeapon.EquipNewWeapon(weaponPrefab);
    }
}
