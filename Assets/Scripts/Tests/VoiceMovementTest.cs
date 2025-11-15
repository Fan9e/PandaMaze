using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class VoiceMovementTest
{
    private class TestObserver : IVoiceObserver
    {
        public List<string> partials = new List<string>();
        public List<string> results = new List<string>();
        public List<float> levels = new List<float>();
        public List<bool> micStates = new List<bool>();

        public void OnPartialResult(string partial) => partials.Add(partial);
        public void OnResult(string result) => results.Add(result);
        public void OnVoiceLevelChanged(float level) => levels.Add(level);
        public void OnMicrophoneStateChanged(bool isOn) => micStates.Add(isOn);
    }

    [UnityTest]
    public IEnumerator StopMicrophone_StopsAndNotifiesObservers()
    {
        var go = new GameObject("VM_StopTest");
        var vm = go.AddComponent<VoiceMovement>();

        var obs = new TestObserver();
        vm.RegisterObserver(obs);

        // Start the microphone (in editor this will immediately start the emulator)
        vm.StartMicrophone();
        // Wait a frame so RequestPermissionAsync callback and Start() complete
        yield return null;
        yield return new WaitForSecondsRealtime(0.05f);

        Assert.IsTrue(vm.isMicrophoneOn, "Microphone should be on after StartMicrophone()");
        Assert.IsTrue(obs.micStates.Contains(true), "Observer should have been notified that mic turned on");

        // Stop microphone explicitly
        vm.StopMicrophone();
        yield return null; // let notifications propagate

        Assert.IsFalse(vm.isMicrophoneOn, "Microphone should be off after StopMicrophone()");
        Assert.IsTrue(obs.micStates.Contains(false), "Observer should have been notified that mic turned off");

        Object.Destroy(go);
    }

    [UnityTest]
    public IEnumerator StartMicrophone_CapturesSpokenText()
    {
        var go = new GameObject("VM_CaptureTest");
        var vm = go.AddComponent<VoiceMovement>();

        var obs = new TestObserver();
        vm.RegisterObserver(obs);

        // Start the microphone -> editor emulator will emit partials and a final "Hello world"
        vm.StartMicrophone();
        yield return null;

        // Wait sufficiently long for the editor emulator sequence to complete (~1.9s in emulator).
        yield return new WaitForSecondsRealtime(3f);

        // Expect that partials and final result were received
        Assert.IsTrue(obs.partials.Count >= 1, "Expected at least one partial result");
        Assert.IsTrue(obs.results.Count >= 1, "Expected at least one final result");
        // The emulator's final spoken text is "Hello world"
        Assert.Contains("Hello world", obs.results, "Final captured text should match the spoken text 'Hello world'");

        Object.Destroy(go);
    }
}

