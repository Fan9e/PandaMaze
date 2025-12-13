using UnityEngine;

public class ChestLoot : IChestLoot
{
    private readonly Weapon weaponPrefab;
    private readonly int keyId;
    private readonly int potionAmount;

    /// <summary>
    /// Opretter loot med våben, nøgle-id og antal potions.
    /// </summary>
    /// <param name="weaponPrefab">Våben prefab der skal equips (kan være null).</param>
    /// <param name="keyId">Id på nøgle der gives til inventory. Brug -1 hvis ingen nøgle.</param>
    /// <param name="potionAmount">Antal potions der gives (negativ bliver til 0).</param>
    public ChestLoot(Weapon weaponPrefab, int keyId, int potionAmount = 0)
    {
        this.weaponPrefab = weaponPrefab;
        this.keyId = keyId;
        this.potionAmount = Mathf.Max(0, potionAmount);
    }

    /// <summary>
    /// Opretter loot uden våben.
    /// </summary>
    /// <param name="keyId">Id på nøgle der gives til inventory. Brug -1 hvis ingen nøgle.</param>
    /// <param name="potionAmount">Antal potions der gives (negativ bliver til 0).</param>
    public ChestLoot(int keyId, int potionAmount = 0)
    : this(null, keyId, potionAmount)
    {
    }

    /// <summary>
    /// Giver loot til spilleren.
    /// </summary>
    /// <param name="inventory">Spillerens inventory (nøgle/potions/weapon).</param>
    public void GiveItemsToPlayer(PlayerInventory inventory)
    {
        if (inventory == null)
        {
            Debug.LogWarning($"{nameof(ChestLoot)}: PlayerInventory er null, kan ikke give loot.");
            return;
        }

       

       if (weaponPrefab != null && inventory.PlayerWeapon != null)
        {
            inventory.PlayerWeapon.EquipNewWeapon(weaponPrefab);
        }

        if (keyId >= 0)
            inventory.AddKey(keyId);

        if (potionAmount > 0)
            inventory.AddPotions(potionAmount);
    }
}
