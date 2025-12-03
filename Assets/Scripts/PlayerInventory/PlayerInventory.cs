using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Holder styr på hvilke nøgler spilleren har samlet op.
/// </summary>
public class PlayerInventory : MonoBehaviour
{
    // Mængde af nøgler spilleren har (ingen dubletter).
    private readonly HashSet<int> _collectedKeys = new HashSet<int>();

    /// <summary>
    /// Tilføj en nøgle til inventory.
    /// </summary>
    public bool AddKey(int keyId)
    {
        if (_collectedKeys.Contains(keyId))
            return false;

        _collectedKeys.Add(keyId);
        Debug.Log($"Spilleren har samlet nøgle {keyId}");
        return true;
    }

    /// <summary>
    /// Tjek om spilleren har en bestemt nøgle.
    /// </summary>
    public bool HasKey(int keyId)
    {
        return _collectedKeys.Contains(keyId);
    }
}

