using System.Collections.Generic;
using UnityEngine;


public class PlayerInventory : MonoBehaviour
{
    /// <summary>Reference til UI’en, så vi kan opdatere nøgle-slot.</summary>
    [SerializeField] private BagpackUI bagpackUI;
  
    /// <summary>
    /// Mængde af nøgler spilleren har samlet op.
    /// HashSet sikrer, at der ikke kan opstå dubletter.
    /// </summary>
    private readonly HashSet<int> _collectedKeys = new HashSet<int>();

    private void Awake()
    {
        // Hvis du glemmer at sætte den i Inspector, prøver vi selv at finde den.
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
        // CASE 1: Hvis dine keyId'er er 0,1,2 i forvejen:
        // return Mathf.Clamp(keyId, 0, 2);

        // CASE 2: Hvis keyId kan være hvad som helst (10, 42, 99 osv.),
        // og du bare vil fordele dem over 3 sprites:
        int variant = Mathf.Abs(keyId) % 3; // giver 0,1 eller 2
        return variant;
    }
}
