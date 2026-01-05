using UnityEngine;

/// <summary>
/// Indeholder loot fra en kiste og kan give det til spilleren (våben, nøgle og potions).
/// </summary>
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
    /// <param name="inventory">Spillerens inventory (nøgle/potions/våben-reference).</param>
    public void GiveItemsToPlayer(PlayerInventory inventory)
    {
        if (!TryValidateInventory(inventory))
            return;

        GiveWeapon(inventory);
        GiveKey(inventory);
        GivePotions(inventory);
    }

    /// <summary>
    /// Tjekker om inventory er gyldig.
    /// </summary>
    private bool TryValidateInventory(PlayerInventory inventory)
    {
        if (inventory != null)
            return true;

        Debug.LogWarning($"{nameof(ChestLoot)}: PlayerInventory er null, kan ikke give loot.");
        return false;
    }

    /// <summary>
    /// Giver våben til spilleren, hvis der findes et weaponPrefab og PlayerWeapon er sat.
    /// </summary>
    private void GiveWeapon(PlayerInventory inventory)
    {
        if (weaponPrefab == null)
            return;

        PlayerWeapon playerWeapon = inventory.PlayerWeapon;
        if (playerWeapon == null)
        {
            Debug.LogWarning($"{nameof(ChestLoot)}: weaponPrefab er sat, men PlayerWeapon mangler på spilleren.");
            return;
        }

        playerWeapon.EquipNewWeapon(weaponPrefab);
    }

    /// <summary>
    /// Giver nøgle til spilleren, hvis keyId er gyldigt.
    /// </summary>
    private void GiveKey(PlayerInventory inventory)
    {
        if (keyId < 0)
            return;

        inventory.AddKey(keyId);
    }

    /// <summary>
    /// Giver potions til spilleren, hvis potionAmount er større end 0.
    /// </summary>
    private void GivePotions(PlayerInventory inventory)
    {
        if (potionAmount <= 0)
            return;

        inventory.AddPotions(potionAmount);
    }

}
