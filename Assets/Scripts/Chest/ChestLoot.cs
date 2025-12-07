using UnityEngine;

public class ChestLoot : IChestLoot
{
    private readonly Weapon weaponPrefab;
    private readonly int keyId;

    public ChestLoot(Weapon weaponPrefab, int keyId)
    {
        this.weaponPrefab = weaponPrefab;
        this.keyId = keyId;
    }

    public void GiveItemsToPlayer(PlayerInventory inventory, PlayerWeapon weapon)
    {
        Debug.Log("ChestLoot: GiveItemsToPlayer. weaponPrefab = " +
                  (weaponPrefab != null ? weaponPrefab.GetType() : "NULL"));

        // Giv våben
        if (weapon != null && weaponPrefab != null)
        {
            Debug.Log("ChestLoot: Equipper nyt våben via PlayerWeapon.EquipNewWeapon(...)");
            weapon.EquipNewWeapon(weaponPrefab);
        }
        else
        {
            if (weapon == null)
                Debug.LogWarning("ChestLoot: PlayerWeapon er null.");
            if (weaponPrefab == null)
                Debug.LogWarning("ChestLoot: weaponPrefab er null.");
        }

        // Giv nøgle
        if (inventory != null)
        {
            Debug.Log("ChestLoot: Giver nøgle med id " + keyId);
            inventory.AddKey(keyId);
        }
        else
        {
            Debug.LogWarning("ChestLoot: PlayerInventory er null.");
        }
    }
}
