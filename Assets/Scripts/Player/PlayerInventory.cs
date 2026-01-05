using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerWeapon))]
public class PlayerInventory : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerWeapon playerWeapon;

    /// <summary>
    /// Reference til PlayerWeapon på spilleren (kan sættes i Inspector eller findes automatisk).
    /// </summary>
    public PlayerWeapon PlayerWeapon => playerWeapon;

    /// <summary>
    /// Reference til UI'en, så inventory kan opdatere key/potion-slot.
    /// </summary>
    [SerializeField] private BagpackUI bagpackUI;

    /// <summary>
    /// Mængde af nøgler spilleren har samlet op.
    /// HashSet sikrer, at der ikke kan opstå dubletter.
    /// </summary>
    [SerializeField] private readonly HashSet<int> _collectedKeys = new HashSet<int>();

    /// <summary>
    /// Kun til debug/inspector-visning. (Opdateres når keys ændrer sig)
    /// </summary>
    [SerializeField] private List<int> _debugKeys = new List<int>();
    [Header("Potions")]
    [SerializeField, Min(0)] private int potionCount = 0;

    /// <summary>
    /// Unity-callback der køres, når objektet initialiseres.
    /// Cacher nødvendige referencer og opdaterer UI'et med den aktuelle tilstand.
    /// </summary>
    private void Awake()
    {
        CacheReferences();
        RefreshUI();
    }

    /// <summary>
    /// Finder/cacher referencer (helst via Inspector, ellers forsøger vi auto-find).
    /// </summary>
    private void CacheReferences()
    {
        InitializeBackpackUI();
        InitializePlayerWeapon();
    }

    /// <summary>
    /// Intialiserer referencen til BagpackUI (enten via Inspector eller auto-find).
    /// </summary>
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
    /// <summary>
    /// Opdaterer alt UI for inventory (keys + potions).
    /// </summary>
    private void RefreshUI()
    {
        UpdateKeyUI();
        UpdatePotionUI();
    }
    
    /// <summary>
    /// Intialiserer referencen til PlayerWeapon (enten via Inspector eller auto-find).
    /// </summary>
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

        UpdateKeyUI(keyId);

        SyncDebugKeys();

        Debug.Log($"Spilleren har samlet nøgle {keyId}");
        return true;
    }

    /// <summary>
    /// Holder debugKeys i sync, så det kan se keys i Inspector.
    /// (Kun til debugging)
    /// </summary>
    private void SyncDebugKeys()
    {
        _debugKeys.Clear();
        _debugKeys.AddRange(_collectedKeys);
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
    /// Opdaterer BagpackUI baseret på de nøgler, spilleren har.
    /// Hvis der ikke er nogen nøgler, skjules ikonet.
    /// Hvis der er mindst én, viser vi varianten ud fra en valgt keyId.
    /// </summary>
    private void UpdateKeyUI(int lastAddedKeyId = -1)
    {
        if (bagpackUI == null)
            return;

        if (_collectedKeys.Count == 0)
        {
            bagpackUI.SetHasKey(false);
            return;
        }

        int keyIdToUse = lastAddedKeyId >= 0 ? lastAddedKeyId : GetAnyKeyId();

        int variant = MapKeyIdToVariant(keyIdToUse);

        bagpackUI.SetHasKey(true);
        bagpackUI.SetKeyVariant(variant);
    }
    /// <summary>
    /// Henter en vilkårlig keyId fra HashSet (hurtig og enkel fallback).
    /// Returnerer -1 hvis der (mod forventning) ingen keys er.
    /// </summary>
    private int GetAnyKeyId()
    {
        foreach (int id in _collectedKeys)
            return id;

        return -1;
    }

    /// <summary>
    /// Oversætter keyId til sprite-variant-index.
    /// Denne mapping antager keyId i området 1..3.
    /// </summary>
    private int MapKeyIdToVariant(int keyId)
    {   
        const int VariantCount = 3;
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
    /// Læser nuværende antal potions.
    /// </summary>
    public int GetPotionCount() => potionCount;

    /// <summary>
    /// Opdaterer BagpackUI’s potion-slot og tekst via det eksisterende BagpackUI.
    /// </summary>
    private void UpdatePotionUI()
    {
        if (bagpackUI == null)
            return;

        bagpackUI.SetPotions(potionCount);
    }

}
