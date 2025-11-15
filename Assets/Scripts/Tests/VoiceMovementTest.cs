using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class VoiceMovementTest
{
    // Small test observer to capture notifications from VoiceMovement.
    class TestObserver : IVoiceObserver
    {
        public string lastPartial;
        public string lastResult;
        public float lastVoiceLevel = -1f;
        public bool? lastMicState;

        public void OnPartialResult(string partial) => lastPartial = partial;
        public void OnResult(string result) => lastResult = result;
        public void OnVoiceLevelChanged(float level) => lastVoiceLevel = level;
        public void OnMicrophoneStateChanged(bool isOn) => lastMicState = isOn;
    }

    // Helper to wait until a condition or timeout (seconds).
    private IEnumerator WaitUntilOrTimeout(System.Func<bool> condition, float timeoutSeconds)
    {
        float start = Time.realtimeSinceStartup;
        while (!condition())
        {
            if (Time.realtimeSinceStartup - start > timeoutSeconds)
                break;
            yield return null;
        }
    }

    [UnityTest]
    public IEnumerator StopsListeningAutomatically_WhenResultArrives()
    {
        // Arrange
        var go = new GameObject("vm");
        var vm = go.AddComponent<VoiceMovement>();
        var observer = new TestObserver();
        vm.RegisterObserver(observer);

        // Act
        vm.StartMicrophone();

        // The editor emulator in SpeechToText runs for ~1.4s; wait up to 3s for end.
        yield return WaitUntilOrTimeout(() => vm.isMicrophoneOn == false || observer.lastMicState == false, 3f);

        // Assert: microphone should have been turned off automatically after result.
        Assert.IsFalse(vm.isMicrophoneOn, "VoiceMovement should stop the microphone automatically after receiving the final result.");
        // Also assert the observer got microphone state notifications (true then false).
        Assert.IsNotNull(observer.lastMicState, "Observer should receive microphone state changes.");
        Assert.IsFalse(observer.lastMicState.Value, "Final microphone state reported to observer should be false.");

        // Clean up
        Object.DestroyImmediate(go);
    }

    [UnityTest]
    public IEnumerator CapturedText_Equals_SpokenText_FromEmulator()
    {
        // Arrange
        var go = new GameObject("vm2");
        var vm = go.AddComponent<VoiceMovement>();
        var observer = new TestObserver();
        vm.RegisterObserver(observer);

        // Act
        vm.StartMicrophone();

        // Wait up to 3s for the final result from the emulator ("Hello world")
        yield return WaitUntilOrTimeout(() => !string.IsNullOrEmpty(observer.lastResult), 3f);

        // Assert
        Assert.IsNotNull(observer.lastResult, "Observer should receive a final result.");
        Assert.AreEqual("Hello world", observer.lastResult, "Captured (final) text should match what the emulator said.");

        // Clean up
        Object.DestroyImmediate(go);
    }
}

