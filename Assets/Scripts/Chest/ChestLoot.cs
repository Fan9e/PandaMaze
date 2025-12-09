using UnityEngine;

public class ChestLoot : IChestLoot
{
    private readonly Weapon weaponPrefab;
    private readonly int keyId;
    private readonly int potionAmount;

    public ChestLoot(Weapon weaponPrefab, int keyId, int potionAmount = 0)
    {
        this.weaponPrefab = weaponPrefab;
        this.keyId = keyId;
        this.potionAmount = Mathf.Max(0, potionAmount);
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

        // Giv nøgle + potions
        if (inventory != null)
        {
            if (keyId >= 0)
            {
                Debug.Log("ChestLoot: Giver nøgle med id " + keyId);
                inventory.AddKey(keyId);
            }

            if (potionAmount > 0)
            {
                Debug.Log("ChestLoot: Giver " + potionAmount + " potions.");
                inventory.AddPotions(potionAmount);
            }
        }
        else
        {
            Debug.LogWarning("ChestLoot: PlayerInventory er null.");
        }
    }
}
