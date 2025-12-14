using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Reflection;

[TestFixture]
public class PlayerInventoryTests
{
    private GameObject _playerGO;
    private PlayerInventory _inventory;
    private BagpackUI _uiMock;

    [SetUp]
    public void SetUp()
    {
    
        LogAssert.Expect(LogType.Error, "Player kunne hverken finde eller oprette noget Weapon.");

        _playerGO = new GameObject("TestPlayer");

        _playerGO.AddComponent<PlayerWeapon>();

        _inventory = _playerGO.AddComponent<PlayerInventory>();


        var uiGO = new GameObject("MockBagpackUI");
        _uiMock = uiGO.AddComponent<BagpackUI>();

        var uiField = typeof(PlayerInventory).GetField(
            "bagpackUI",
            BindingFlags.NonPublic | BindingFlags.Instance
        );
        uiField.SetValue(_inventory, _uiMock);
    }

    [TearDown]
    public void TearDown()
    {
        if (_playerGO != null) Object.DestroyImmediate(_playerGO);
        if (_uiMock != null) Object.DestroyImmediate(_uiMock.gameObject);
    }

    [Test]
    public void AddKey_ReturnsTrue_WhenKeyNotPresent()
    {
        var added = _inventory.AddKey(1);

        Assert.IsTrue(added);
        Assert.IsTrue(_inventory.HasKey(1));
    }

    [Test]
    public void AddKey_ReturnsFalse_WhenKeyAlreadyPresent()
    {
        Assert.IsTrue(_inventory.AddKey(2));
        Assert.IsFalse(_inventory.AddKey(2));
    }

    [Test]
    public void HasKey_ReturnsFalse_WhenKeyNotPresent()
    {
        Assert.IsFalse(_inventory.HasKey(999));
    }

    [Test]
    public void AddingMultipleDistinctKeys_WorksAndPreventsDuplicates()
    {
        Assert.IsTrue(_inventory.AddKey(10));
        Assert.IsTrue(_inventory.AddKey(20));

        Assert.IsTrue(_inventory.HasKey(10));
        Assert.IsTrue(_inventory.HasKey(20));

        Assert.IsFalse(_inventory.AddKey(10));
    }


    [Test]
    public void AddPotions_IncreasesCount_WhenAmountPositive()
    {
        Assert.AreEqual(0, _inventory.GetPotionCount());

        _inventory.AddPotions(3);

        Assert.AreEqual(3, _inventory.GetPotionCount());
    }

    [Test]
    public void AddPotions_DoesNothing_WhenAmountZeroOrNegative()
    {
        _inventory.AddPotions(2);
        Assert.AreEqual(2, _inventory.GetPotionCount());

        _inventory.AddPotions(0);
        Assert.AreEqual(2, _inventory.GetPotionCount());

        _inventory.AddPotions(-5);
        Assert.AreEqual(2, _inventory.GetPotionCount());
    }
}
