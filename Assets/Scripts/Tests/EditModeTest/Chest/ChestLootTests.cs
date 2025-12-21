using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;


public class ChestLootTests
{
    private const string InventoryNullWarning = "ChestLoot: PlayerInventory er null, kan ikke give loot.";
    private const string MissingPlayerWeaponWarning = "ChestLoot: weaponPrefab er sat, men PlayerWeapon mangler på spilleren.";

    /// <summary>
    /// Verificerer at loot giver key og potions til inventory.
    /// </summary>
    [Test]
    public void GiveItemsToPlayer_GivesKeyAndPotions_WhenKeyIdAndPotionAmountAreValid()
    {
        GameObject inventoryGO = new GameObject();
        PlayerInventory inventory = inventoryGO.AddComponent<PlayerInventory>();

        ChestLoot loot = new ChestLoot(null, 1, 2);
        loot.GiveItemsToPlayer(inventory);

        Assert.IsTrue(inventory.HasKey(1));
        Assert.AreEqual(2, inventory.GetPotionCount());
    }


    /// <summary>
    /// Verificerer at GiveItemsToPlayer logger warning og stopper, hvis inventory er null.
    /// </summary>
    [Test]
    public void GiveItemsToPlayer_LogsWarning_WhenInventoryIsNull()
    {
        ChestLoot loot = new ChestLoot(null, 1, 1);

        LogAssert.Expect(LogType.Warning, InventoryNullWarning);
        loot.GiveItemsToPlayer(null);

        LogAssert.NoUnexpectedReceived();
    }

    /// <summary>
    /// Verificerer at negative potionAmount clamped til 0, så potion count ikke ændres.
    /// </summary>
    [Test]
    public void GiveItemsToPlayer_DoesNotAddPotions_WhenPotionAmountIsNegative()
    {
        GameObject inventoryGO = new GameObject();
        PlayerInventory inventory = inventoryGO.AddComponent<PlayerInventory>();

        int before = inventory.GetPotionCount();

        ChestLoot loot = new ChestLoot(null, 1, -10);
        loot.GiveItemsToPlayer(inventory);

        Assert.IsTrue(inventory.HasKey(1));
        Assert.AreEqual(before, inventory.GetPotionCount());
    }

    /// <summary>
    /// Verificerer at key ikke gives, hvis keyId er mindre end 0, men potions stadig gives.
    /// </summary>
    [Test]
    public void GiveItemsToPlayer_DoesNotGiveKey_WhenKeyIdIsNegative()
    {
        GameObject inventoryGO = new GameObject();
        PlayerInventory inventory = inventoryGO.AddComponent<PlayerInventory>();

        ChestLoot loot = new ChestLoot(null, -1, 3);
        loot.GiveItemsToPlayer(inventory);

        Assert.IsFalse(inventory.HasKey(-1));
        Assert.AreEqual(3, inventory.GetPotionCount());
    }

    /// <summary>
    /// Verificerer at potionAmount på 0 ikke ændrer potion count.
    /// </summary>
    [Test]
    public void GiveItemsToPlayer_DoesNotAddPotions_WhenPotionAmountIsZero()
    {
        GameObject inventoryGO = new GameObject();
        PlayerInventory inventory = inventoryGO.AddComponent<PlayerInventory>();

        int before = inventory.GetPotionCount();

        ChestLoot loot = new ChestLoot(null, 1, 0);
        loot.GiveItemsToPlayer(inventory);

        Assert.IsTrue(inventory.HasKey(1));
        Assert.AreEqual(before, inventory.GetPotionCount());
    }
}
