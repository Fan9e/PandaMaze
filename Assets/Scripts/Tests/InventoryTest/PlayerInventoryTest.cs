using NUnit.Framework;
using UnityEngine;

[TestFixture]
public class PlayerInventoryTests
{
    private GameObject _go;
    private PlayerInventory _inventory;

    [SetUp]
    public void SetUp()
    {
        _go = new GameObject("TestPlayer");
        _inventory = _go.AddComponent<PlayerInventory>();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_go);
    }

    [Test]
    public void AddKey_ReturnsTrue_WhenKeyNotPresent()
    {
        var added = _inventory.AddKey(1);
        Assert.IsTrue(added, "AddKey should return true when adding a new key.");
        Assert.IsTrue(_inventory.HasKey(1), "HasKey should return true after adding the key.");
    }

    [Test]
    public void AddKey_ReturnsFalse_WhenKeyAlreadyPresent()
    {
        Assert.IsTrue(_inventory.AddKey(2), "First AddKey should succeed.");
        Assert.IsFalse(_inventory.AddKey(2), "Second AddKey for same id should return false.");
    }

    [Test]
    public void HasKey_ReturnsFalse_WhenKeyNotPresent()
    {
        Assert.IsFalse(_inventory.HasKey(999), "HasKey should return false for keys that were not added.");
    }

    [Test]
    public void AddingMultipleDistinctKeys_WorksAndPreventsDuplicates()
    {
        Assert.IsTrue(_inventory.AddKey(10));
        Assert.IsTrue(_inventory.AddKey(20));
        Assert.IsTrue(_inventory.HasKey(10));
        Assert.IsTrue(_inventory.HasKey(20));
        Assert.IsFalse(_inventory.AddKey(10), "Re-adding an existing key must return false (no duplicate).");
    }
}