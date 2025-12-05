using UnityEngine;

public class Key : Item
{
    [SerializeField]
    [Tooltip("ID på denne nøgle. Skal matche den dør, den kan åbne.")]
    private int keyId = 1;

    /// <summary>
    /// Håndterer, når en collider rammer nøglens trigger.
    /// Flytter logikken til en privat hjælpermetode for bedre læsbarhed og testbarhed.
    /// </summary>
    /// <param name="other">Den collider, der rammer nøglens trigger.</param>
    private void OnTriggerEnter(Collider other)
    {
        TryPickupKey(other);
    }

    /// <summary>
    /// Privat hjælpermetode, der håndterer opsamling af nøglen når spilleren rammer triggeren.
    /// Forsøger at finde spillerens inventory og tilføje nøglen; fjerner objektet ved succes.
    /// </summary>
    /// <param name="other">Den collider, der rammer nøglens trigger.</param>
    private void TryPickupKey(Collider other)
    {
        // Kun spilleren må samle nøglen op
        if (!other.CompareTag("Player"))
            return;

        // Forsøg at finde spillerens inventory
        var inventory = other.GetComponent<PlayerInventory>();
        if (inventory == null)
            return;

        // Tilføj nøgle til inventory
        bool pickedUp = inventory.AddKey(keyId);
        if (pickedUp)
        {
            // Fjern nøglen fra verden, når den er samlet op
            Destroy(gameObject);
        }
    }
}