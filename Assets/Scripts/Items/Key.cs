using UnityEngine;

public class Key : Item
{
    [SerializeField]
    [Tooltip("ID på denne nøgle. Skal matche den dør, den kan åbne.")]
    private int keyId = 1;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        var inventory = other.GetComponent<PlayerInventory>();
        if (inventory == null)
            return;

        bool pickedUp = inventory.AddKey(keyId);
        if (pickedUp)
        {
            // TODO: Spil lyd / vis UI her hvis du vil
            Destroy(gameObject); // fjern nøglen fra verden
        }
    }
}
