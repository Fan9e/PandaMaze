using NUnit.Framework;
using UnityEngine;

public class PlayerMovementTests
{
    private GameObject playerObject;
    private PlayerMovement player;
    private Rigidbody rb;

    [SetUp]
    public void Setup()
    {
        playerObject = new GameObject("Player");

        // Lav rigidbody først
        rb = playerObject.AddComponent<Rigidbody>();

        // Tilføj PlayerMovement
        player = playerObject.AddComponent<PlayerMovement>();

        // Kobl rigidbody’en direkte på komponenten
        player.RigidbodyComponent = rb;

        // Start-værdi på Y-hastighed
        rb.linearVelocity = new Vector3(0f, 3f, 0f);

        // Sæt farten
        player.MovementSpeed = 2f;
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(playerObject);
    }

    [Test]
    public void ApplyMovement_SetsHorizontalVelocity_BasedOnMovementSpeed()
    {
        player.SetMovementInput(new Vector3(1f, 0f, 0f));
        var expectedX = player.MovementSpeed;

        player.ApplyMovement();

        Assert.AreEqual(expectedX, rb.linearVelocity.x, 0.0001f);
    }

    [Test]
    public void ApplyMovement_PreservesExistingYVelocity()
    {
        player.SetMovementInput(new Vector3(1f, 0f, 0f));
        var beforeY = rb.linearVelocity.y;

        player.ApplyMovement();

        Assert.AreEqual(beforeY, rb.linearVelocity.y, 0.0001f);
    }
}
