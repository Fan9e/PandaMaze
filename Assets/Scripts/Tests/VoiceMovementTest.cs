using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// Enhedstestklasse for <see cref="VoiceMovement"/>-komponenten.
/// Indeholder en intern observer-implementering og UnityTest-scenarier
/// der validerer mikrofonens adfærd og tekstfangst.
/// Gør brug af emulatoren fra yasirkula's SpeechToText
/// til at simulere taleinput.
/// </summary>
public class VoiceMovementTest
{
    /// <summary>
    /// Enkel testimplementering af <see cref="IVoiceObserver"/>.
    /// Holder på de seneste værdier som modtages fra VoiceMovement,
    /// så testene kan validere dem.
    /// </summary>
    class TestObserver : IVoiceObserver
    {
        /// <summary>
        /// Seneste delvise (partial) transskription modtaget.
        /// </summary>
        public string lastPartial;

        /// <summary>
        /// Seneste endelige (final) transskription modtaget.
        /// </summary>
        public string lastResult;

        /// <summary>
        /// Seneste rapporterede lydniveau (voice level). Standard -1 indikerer at ingen værdi er modtaget endnu.
        /// </summary>
        public float lastVoiceLevel = -1f;

        /// <summary>
        /// Seneste kendte mikrofontilstand; true = tændt, false = slukket, null = ingen opdatering endnu.
        /// </summary>
        public bool? lastMicState;

        /// <summary>
        /// Modtager delvise resultater (partial).
        /// </summary>
        /// <param name="partial">Den delvise transskription.</param>
        public void OnPartialResult(string partial) => lastPartial = partial;

        /// <summary>
        /// Modtager det endelige resultat.
        /// </summary>
        /// <param name="result">Den endelige transskription.</param>
        public void OnResult(string result) => lastResult = result;

        /// <summary>
        /// Modtager opdateringer af lydniveau.
        /// </summary>
        /// <param name="level">Det rapporterede lydniveau.</param>
        public void OnVoiceLevelChanged(float level) => lastVoiceLevel = level;

        /// <summary>
        /// Modtager ændringer i mikrofontilstand.
        /// </summary>
        /// <param name="isOn">True hvis mikrofonen er tændt, ellers false.</param>
        public void OnMicrophoneStateChanged(bool isOn) => lastMicState = isOn;
    }

    /// <summary>
    /// Hjælpe-metode til UnityTest: venter indtil <paramref name="condition"/> er sand
    /// eller indtil <paramref name="timeoutSeconds"/> er passeret.
    /// </summary>
    /// <param name="condition">Predicate der skal blive sand for at stoppe ventetiden.</param>
    /// <param name="timeoutSeconds">Maksimum ventetid i sekunder.</param>
    /// <returns>En IEnumerator egnet til brug i en UnityTest.</returns>
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

    /// <summary>
    /// Test: Verificerer at mikrofonen stoppes automatisk når et endeligt resultat modtages.
    /// Starter mikrofonen, venter indtil mikrofonen rapporteres som slukket eller observer modtager slukket tilstand,
    /// og validerer både komponent-tilstanden og observer-opdateringen.
    /// </summary>
    /// <returns>IEnumerator så testen kan køres som UnityTest.</returns>
    [UnityTest]
    public IEnumerator StopsListeningAutomatically_WhenResultArrives()
    {
        var go = new GameObject("vm");
        var vm = go.AddComponent<VoiceMovement>();
        var observer = new TestObserver();
        vm.RegisterObserver(observer);

        vm.StartMicrophone();

        yield return WaitUntilOrTimeout(() => vm.isMicrophoneOn == false || observer.lastMicState == false, 3f);


        Assert.IsFalse(vm.isMicrophoneOn, "VoiceMovement should stop the microphone automatically after receiving the final result.");

        Assert.IsNotNull(observer.lastMicState, "Observer should receive microphone state changes.");
        Assert.IsFalse(observer.lastMicState.Value, "Final microphone state reported to observer should be false.");

        Object.DestroyImmediate(go);
    }

    /// <summary>
    /// Test: Verificerer at den optagede (endelige) tekst svarer til det som emulatoren forventes at sige ("Hello world").
    /// Starter mikrofonen og venter indtil observer modtager et endeligt resultat.
    /// </summary>
    /// <returns>IEnumerator så testen kan køres som UnityTest.</returns>
    [UnityTest]
    public IEnumerator CapturedText_Equals_SpokenText_FromEmulator()
    {
        var go = new GameObject("vm2");
        var vm = go.AddComponent<VoiceMovement>();
        var observer = new TestObserver();
        vm.RegisterObserver(observer);

        vm.StartMicrophone();

        yield return WaitUntilOrTimeout(() => !string.IsNullOrEmpty(observer.lastResult), 3f);

        Assert.IsNotNull(observer.lastResult, "Observer should receive a final result.");
        Assert.AreEqual("Hello world", observer.lastResult, "Captured (final) text should match what the emulator said.");

        Object.DestroyImmediate(go);
    }
}

