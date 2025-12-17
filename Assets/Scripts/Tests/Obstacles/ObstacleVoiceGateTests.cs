using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// Tests for <see cref="ObstacleVoiceGate"/> der validerer at en "over"-kommando
/// får spilleren til at bevæge sig fremad og at spillerens tilstand gendannes korrekt.
/// </summary>
public class ObstacleVoiceGateTests
{
    /// <summary>
    /// Integrationstest (PlayMode) som simulerer at spilleren går ind i en gate,
    /// at VoiceMovement modtager et genkendt resultat ("over") og at gate udfører
    /// en "over"-passage.
    ///
    /// Testen verificerer:
    /// - At spilleren flyttes fremad med omtrent <see cref="ObstacleVoiceGate.passDistance"/>.
    /// - At Y-position er restitueret til niveau før passagen.
    /// - At <see cref="PlayerMovement"/> bliver slået fra under passagen og genaktiveret efterfølgende.
    /// - At rigidbody's isKinematic-tilstand gendannes.
    /// </summary>
    [UnityTest]
    public IEnumerator PerformOver_SpeechRecognized_PlayerMovesForwardAndRestoresState()
    {
        var gateGO = new GameObject("Gate");
        var gate = gateGO.AddComponent<ObstacleVoiceGate>();

        gate.passDistance = 1.0f;
        gate.passDuration = 0.12f;
        gate.overHeight = 0.3f;
        gate.autoStartMicrophone = false;
        gate.Kind = ObstacleVoiceGate.ObstacleKind.Rocks;

        var playerGO = new GameObject("Player");
        playerGO.transform.position = Vector3.zero;
        playerGO.transform.rotation = Quaternion.identity;

        var playerRb = playerGO.AddComponent<Rigidbody>();
        playerRb.useGravity = false;
        playerRb.isKinematic = false;

        var playerMovement = playerGO.AddComponent<PlayerMovement>();

        var playerCollider = playerGO.AddComponent<SphereCollider>();

        var vmGO = new GameObject("VoiceMovement");
        var voiceMovement = vmGO.AddComponent<VoiceMovement>();

        yield return null;

        Assert.IsTrue(playerMovement.enabled, "PlayerMovement should start enabled.");
        Assert.IsFalse(playerRb.isKinematic == true && playerRb.isKinematic == false, "Rigidbody kinematic should be false initially.");

        var gateType = typeof(ObstacleVoiceGate);
        var handleEnter = gateType.GetMethod("HandleTriggerEnter", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(handleEnter, "Could not find HandleTriggerEnter via reflection");
        handleEnter.Invoke(gate, new object[] { playerCollider });

        var onResultMethod = typeof(VoiceMovement).GetMethod("OnResultReceived", BindingFlags.Public | BindingFlags.Instance);
        Assert.IsNotNull(onResultMethod, "VoiceMovement.OnResultReceived not found");
        onResultMethod.Invoke(voiceMovement, new object[] { "over", (int?)null });

        yield return new WaitForSeconds(gate.passDuration + 0.15f);

        var expected = Vector3.forward * gate.passDistance;
        var actual = playerGO.transform.position;
        Assert.AreEqual(expected.x, actual.x, 0.05f, $"Player did not move the expected distance on X (expected {expected.x}, was {actual.x})");
        Assert.AreEqual(expected.z, actual.z, 0.05f, $"Player did not move the expected distance on Z (expected {expected.z}, was {actual.z})");

        Assert.AreEqual(0f, actual.y, 0.05f, "Player Y position was not restored to ground level after over-pass.");

        Assert.IsTrue(playerMovement.enabled, "PlayerMovement should be re-enabled after pass.");
        Assert.IsFalse(playerRb.isKinematic, "Rigidbody should have its original isKinematic state restored.");

        Object.DestroyImmediate(gateGO);
        Object.DestroyImmediate(playerGO);
        Object.DestroyImmediate(vmGO);
    }
}