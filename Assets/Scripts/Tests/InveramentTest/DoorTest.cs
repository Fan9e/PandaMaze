using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class DoorTests
{
    private GameObject _doorGo;
    private Door _doorComp;

    [TearDown]
    public void TearDown()
    {
        if (_doorGo != null)
            Object.DestroyImmediate(_doorGo);
    }

    [UnityTest]
    public IEnumerator Awake_SetsColliderAndDefaultVisual()
    {
        // Arrange: create GameObject with Door and a Collider, but do not set doorVisual
        _doorGo = new GameObject("Door_AwakeTest");
        _doorGo.AddComponent<BoxCollider>(); // required by RequireComponent
        _doorComp = _doorGo.AddComponent<Door>();

        // Wait a frame so Awake runs
        yield return null;

        // Use reflection to read private fields
        var type = typeof(Door);
        var doorVisualField = type.GetField("doorVisual", BindingFlags.NonPublic | BindingFlags.Instance);
        var colliderField = type.GetField("_collider", BindingFlags.NonPublic | BindingFlags.Instance);

        var doorVisualValue = doorVisualField.GetValue(_doorComp) as GameObject;
        var colliderValue = colliderField.GetValue(_doorComp) as Collider;

        // Assert that doorVisual defaulted to the GameObject itself and _collider was set
        Assert.IsNotNull(colliderValue, "_collider should be set in Awake");
        Assert.AreEqual(_doorGo, doorVisualValue, "doorVisual should default to the Door's GameObject when not assigned");
    }

    [Test]
    public void OpenDoor_DisablesColliderAndHidesVisual()
    {
        // Arrange
        _doorGo = new GameObject("Door_OpenDoorTest");
        var box = _doorGo.AddComponent<BoxCollider>();
        _doorComp = _doorGo.AddComponent<Door>();

        // Create a separate visual GameObject and assign it via reflection
        var visual = new GameObject("DoorVisual");
        visual.SetActive(true);

        var type = typeof(Door);
        var doorVisualField = type.GetField("doorVisual", BindingFlags.NonPublic | BindingFlags.Instance);
        var colliderField = type.GetField("_collider", BindingFlags.NonPublic | BindingFlags.Instance);

        doorVisualField.SetValue(_doorComp, visual);
        // Force Awake-like collider assignment if not already assigned
        colliderField.SetValue(_doorComp, _doorGo.GetComponent<Collider>());

        // Pre-assert
        Assert.IsTrue(_doorGo.GetComponent<Collider>().enabled, "Collider should be enabled before OpenDoor");
        Assert.IsTrue(visual.activeSelf, "Visual should be active before OpenDoor");

        // Act: call private OpenDoor via reflection
        var openMethod = type.GetMethod("OpenDoor", BindingFlags.NonPublic | BindingFlags.Instance);
        openMethod.Invoke(_doorComp, null);

        // Assert
        Assert.IsFalse(_doorGo.GetComponent<Collider>().enabled, "Collider should be disabled after OpenDoor");
        Assert.IsFalse(visual.activeSelf, "Visual should be inactive after OpenDoor");
    }

    [UnityTest]
    public IEnumerator OnCollisionEnter_WithKey_OpensDoor()
    {
        // Arrange door
        _doorGo = new GameObject("Door_Collision_Open");
        var doorCollider = _doorGo.AddComponent<BoxCollider>();
        _doorComp = _doorGo.AddComponent<Door>();

        // Create visual and assign
        var visual = new GameObject("DoorVisual_Collision_Open");
        var type = typeof(Door);
        var doorVisualField = type.GetField("doorVisual", BindingFlags.NonPublic | BindingFlags.Instance);
        doorVisualField.SetValue(_doorComp, visual);

        // Ensure collider field is set
        var colliderField = type.GetField("_collider", BindingFlags.NonPublic | BindingFlags.Instance);
        colliderField.SetValue(_doorComp, _doorGo.GetComponent<Collider>());

        // Arrange player
        var player = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        player.name = "Player_WithKey";
        // Expecting project to have "Player" tag; ensure tag exists in project Tag Manager
        player.tag = "Player";

        var rb = player.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.isKinematic = false;

        // Add PlayerInventory and give the correct key (default doorId is 1)
        var inventory = player.AddComponent<PlayerInventory>();
        inventory.AddKey(1);

        // Position player so it will move into door
        player.transform.position = new Vector3(0, 0, -3f);
        _doorGo.transform.position = Vector3.zero;

        // Give velocity toward the door
        rb.linearVelocity = new Vector3(0, 0, 10f);

        // Wait several fixed updates to allow collision / message processing
        int fixedFramesToWait = 30;
        for (int i = 0; i < fixedFramesToWait; i++)
            yield return new WaitForFixedUpdate();

        // Assert door opened
        Assert.IsFalse(_doorGo.GetComponent<Collider>().enabled, "Door collider should be disabled after player with key collides");
        Assert.IsFalse(visual.activeSelf, "Door visual should be inactive after player with key collides");

        // Cleanup player
        Object.DestroyImmediate(player);
    }

    [UnityTest]
    public IEnumerator OnCollisionEnter_WithoutKey_KeepsDoorClosed()
    {
        // Arrange door
        _doorGo = new GameObject("Door_Collision_Closed");
        var doorCollider = _doorGo.AddComponent<BoxCollider>();
        _doorComp = _doorGo.AddComponent<Door>();

        // Create visual and assign
        var visual = new GameObject("DoorVisual_Collision_Closed");
        var type = typeof(Door);
        var doorVisualField = type.GetField("doorVisual", BindingFlags.NonPublic | BindingFlags.Instance);
        doorVisualField.SetValue(_doorComp, visual);

        // Ensure collider field is set
        var colliderField = type.GetField("_collider", BindingFlags.NonPublic | BindingFlags.Instance);
        colliderField.SetValue(_doorComp, _doorGo.GetComponent<Collider>());

        // Arrange player without key
        var player = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        player.name = "Player_NoKey";
        player.tag = "Player";

        var rb = player.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.isKinematic = false;

        // No PlayerInventory.AddKey call here: player lacks the key
        var inventory = player.AddComponent<PlayerInventory>();

        // Position player and give velocity toward the door
        player.transform.position = new Vector3(0, 0, -3f);
        _doorGo.transform.position = Vector3.zero;
        rb.linearVelocity = new Vector3(0, 0, 10f);

        // Wait several fixed updates to allow collision
        int fixedFramesToWait = 30;
        for (int i = 0; i < fixedFramesToWait; i++)
            yield return new WaitForFixedUpdate();

        // Assert door still closed
        Assert.IsTrue(_doorGo.GetComponent<Collider>().enabled, "Door collider should remain enabled when player lacks the key");
        Assert.IsTrue(visual.activeSelf, "Door visual should remain active when player lacks the key");

        // Cleanup player
        Object.DestroyImmediate(player);
    }
}
