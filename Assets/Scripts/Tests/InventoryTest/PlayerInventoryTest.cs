using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

[TestFixture]
public class PlayerInventoryTests
{
    private GameObject _go;
    private PlayerInventory _inventory;
    private BagpackUI _mockUI;

    [SetUp]
    public void SetUp()
    {
        LogAssert.ignoreFailingMessages = true;

        _go = new GameObject("TestPlayer");
        _go.AddComponent<PlayerWeapon>();
        _inventory = _go.AddComponent<PlayerInventory>();

        var uiGO = new GameObject("MockBagpackUI");
        _mockUI = uiGO.AddComponent<BagpackUI>();

        var uiField = typeof(PlayerInventory)
            .GetField("bagpackUI", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        uiField.SetValue(_inventory, _mockUI);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_go);
        if (_mockUI != null)
            Object.DestroyImmediate(_mockUI.gameObject);
    }

    [Test]
    public void AddKey_ReturnsTrue_WhenKeyNotPresent()
    {
        var added = _inventory.AddKey(1);

        Assert.IsTrue(added, "AddKey skal returnere true, når man tilføjer en ny nøgle.");
        Assert.IsTrue(_inventory.HasKey(1), "HasKey skal returnere true efter nøglen er tilføjet.");
    }

    [Test]
    public void AddKey_ReturnsFalse_WhenKeyAlreadyPresent()
    {
        Assert.IsTrue(_inventory.AddKey(2), "Første AddKey skal returnere true.");
        Assert.IsFalse(_inventory.AddKey(2), "Anden AddKey med samme ID skal returnere false.");
    }

    [Test]
    public void HasKey_ReturnsFalse_WhenKeyNotPresent()
    {
        Assert.IsFalse(_inventory.HasKey(999), "HasKey skal returnere false for nøgler der ikke findes.");
    }

    [Test]
    public void AddingMultipleDistinctKeys_WorksAndPreventsDuplicates()
    {
        Assert.IsTrue(_inventory.AddKey(10));
        Assert.IsTrue(_inventory.AddKey(20));
        Assert.IsTrue(_inventory.HasKey(10));
        Assert.IsTrue(_inventory.HasKey(20));
        Assert.IsFalse(_inventory.AddKey(10), "Samme nøgle må ikke kunne tilføjes to gange.");
    }
}