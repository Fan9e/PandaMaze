using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class KeyTests
{
    private const string PlayerTag = "Player";

    private MethodInfo _onTriggerEnterMethod;

    [SetUp]
    public void SetUp()
    {
        _onTriggerEnterMethod = typeof(Key).GetMethod("OnTriggerEnter", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(_onTriggerEnterMethod, "Could not find private OnTriggerEnter method via reflection.");
    }

    [TearDown]
    public void TearDown()
    {
        // Clean up any leftover GameObjects in the scene to avoid cross-test pollution
        foreach (var go in Object.FindObjectsOfType<GameObject>())
        {
            Object.DestroyImmediate(go);
        }
    }

    // Helper to invoke the private OnTriggerEnter method
    private void InvokeOnTriggerEnter(Key key, Collider collider)
    {
        _onTriggerEnterMethod.Invoke(key, new object[] { collider });
    }

    [UnityTest]
    public IEnumerator OnTriggerEnter_Ignores_WhenColliderIsNotPlayer()
    {
        // Arrange
        var keyGo = new GameObject("Key");
        keyGo.AddComponent<BoxCollider>().isTrigger = true;
        var key = keyGo.AddComponent<Key>();

        var other = new GameObject("NotPlayer");
        other.AddComponent<BoxCollider>();
        // ensure tag is not Player (default is "Untagged")
        other.tag = "Untagged";

        var otherCollider = other.GetComponent<Collider>();

        // Act
        InvokeOnTriggerEnter(key, otherCollider);
        yield return null; // allow Unity to process any Destroy calls

        // Assert - key should still exist
        Assert.IsFalse(key == null, "Key should not be destroyed when a non-player collider triggers it.");
    }

    [UnityTest]
    public IEnumerator OnTriggerEnter_Ignores_WhenPlayerHasNoInventory()
    {
        // Arrange
        var keyGo = new GameObject("Key");
        keyGo.AddComponent<BoxCollider>().isTrigger = true;
        var key = keyGo.AddComponent<Key>();

        var player = new GameObject("Player");
        player.AddComponent<BoxCollider>();
        player.tag = PlayerTag;
        var playerCollider = player.GetComponent<Collider>();
        // Note: no PlayerInventory component attached

        // Act
        InvokeOnTriggerEnter(key, playerCollider);
        yield return null;

        // Assert - key should still exist
        Assert.IsFalse(key == null, "Key should not be destroyed when the player has no PlayerInventory component.");
    }

    [UnityTest]
    public IEnumerator OnTriggerEnter_AddsKeyAndDestroys_WhenInventoryAcceptsKey()
    {
        // Arrange
        var keyGo = new GameObject("Key");
        keyGo.AddComponent<BoxCollider>().isTrigger = true;
        var key = keyGo.AddComponent<Key>();

        var player = new GameObject("PlayerWithInventory");
        player.AddComponent<BoxCollider>();
        player.tag = PlayerTag;
        var inventory = player.AddComponent<PlayerInventory>();
        var playerCollider = player.GetComponent<Collider>();

        // Retrieve keyId BEFORE the key GameObject can be destroyed by the tested code
        var keyIdField = typeof(Key).GetField("keyId", BindingFlags.NonPublic | BindingFlags.Instance);
        int keyId = (int)keyIdField.GetValue(key);

        // Act
        InvokeOnTriggerEnter(key, playerCollider);
        // Destroy is processed at end of frame; allow one frame
        yield return null;

        // Assert - key should be destroyed (picked up)
        Assert.IsTrue(key == null, "Key should be destroyed when the player's inventory accepts the key.");
        // Also assert inventory now has the key
        Assert.IsTrue(inventory.HasKey(keyId));
    }

    [UnityTest]
    public IEnumerator OnTriggerEnter_DoesNotDestroy_WhenInventoryAlreadyHasKey()
    {
        // Arrange
        var keyGo = new GameObject("Key");
        keyGo.AddComponent<BoxCollider>().isTrigger = true;
        var key = keyGo.AddComponent<Key>();

        var player = new GameObject("PlayerWithInventory");
        player.AddComponent<BoxCollider>();
        player.tag = PlayerTag;
        var inventory = player.AddComponent<PlayerInventory>();
        var playerCollider = player.GetComponent<Collider>();

        // Pre-add the key id to the inventory's private _collectedKeys so AddKey returns false
        var keyIdField = typeof(Key).GetField("keyId", BindingFlags.NonPublic | BindingFlags.Instance);
        int keyId = (int)keyIdField.GetValue(keyGo.GetComponent<Key>());

        var collectedField = typeof(PlayerInventory).GetField("_collectedKeys", BindingFlags.NonPublic | BindingFlags.Instance);
        var collectedSet = (HashSet<int>)collectedField.GetValue(inventory);
        collectedSet.Add(keyId);

        // Sanity check - inventory already has the key
        Assert.IsTrue(inventory.HasKey(keyId));

        // Act
        InvokeOnTriggerEnter(key, playerCollider);
        yield return null;

        // Assert - key should still exist because AddKey returned false
        Assert.IsFalse(key == null, "Key should not be destroyed when the player already has the same key in inventory.");
    }
}