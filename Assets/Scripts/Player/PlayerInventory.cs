using System.Collections.Generic;
using UnityEngine;


public class PlayerInventory : MonoBehaviour
{
    /// <summary>
    /// Mængde af nøgler spilleren har samlet op.
    /// HashSet sikrer, at der ikke kan opstå dubletter.
    /// </summary>
    private readonly HashSet<int> _collectedKeys = new HashSet<int>();

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
}
