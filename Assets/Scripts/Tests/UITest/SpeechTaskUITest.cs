using System.Collections;
using NUnit.Framework;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.TestTools;

public class SpeechTaskUITest
{
    private GameObject go;
    private SpeechTaskUI ui;

    private const BindingFlags PrivateInstance =
        BindingFlags.Instance | BindingFlags.NonPublic;

    // Hjælper: læs tekstfeltet (TMP) via reflection
    private static string GetTextFromPrivateField(SpeechTaskUI target, string fieldName)
    {
        var field = typeof(SpeechTaskUI).GetField(fieldName, PrivateInstance);
        var obj = field.GetValue(target);
        if (obj == null) return null;

        var textProp = obj.GetType().GetProperty("text");
        return (string)textProp.GetValue(obj);
    }

    [SetUp]
    public void SetUp()
    {
        go = new GameObject("SpeechTaskUI");
        ui = go.AddComponent<SpeechTaskUI>();

        var uiType = typeof(SpeechTaskUI);

        var panelField = uiType.GetField("panel", PrivateInstance);
        var instructField = uiType.GetField("instructionText", PrivateInstance);
        var sentenceField = uiType.GetField("sentenceText", PrivateInstance);
        var feedbackField = uiType.GetField("feedbackText", PrivateInstance);

        // Panel
        var panelGO = new GameObject("Panel");
        panelGO.transform.SetParent(go.transform, false);
        panelField.SetValue(ui, panelGO);

        // Instruction TMP
        var instrGO = new GameObject("Instruction");
        instrGO.transform.SetParent(panelGO.transform, false);
        var instrComponent = instrGO.AddComponent(instructField.FieldType);
        instructField.SetValue(ui, instrComponent);

        // Sentence TMP
        var sentenceGO = new GameObject("Sentence");
        sentenceGO.transform.SetParent(panelGO.transform, false);
        var sentenceComponent = sentenceGO.AddComponent(sentenceField.FieldType);
        sentenceField.SetValue(ui, sentenceComponent);

        // Feedback TMP
        var feedbackGO = new GameObject("Feedback");
        feedbackGO.transform.SetParent(panelGO.transform, false);
        var feedbackComponent = feedbackGO.AddComponent(feedbackField.FieldType);
        feedbackField.SetValue(ui, feedbackComponent);

        // Kend default-sætning
        uiType.GetField("defaultSentence", PrivateInstance)
              .SetValue(ui, "Jeg har set en pirat");

        // Panel starter skjult
        panelGO.SetActive(false);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(go);
    }

    /// <summary>
    /// Tester at Normalize fjerner tegnsætning, laver tekst til lowercase
    /// og håndterer null/whitespace korrekt.
    /// </summary>
    [Test]
    public void Normalize_RemovesPunctuationAndLowercasesAndTrims()
    {
        var normalize = typeof(SpeechTaskUI)
            .GetMethod("Normalize", PrivateInstance);
        Assert.IsNotNull(normalize);

        string input = "  HeLLo, World!  ";
        var result = (string)normalize.Invoke(ui, new object[] { input });
        Assert.AreEqual("hello world", result);

        Assert.AreEqual("", (string)normalize.Invoke(ui, new object[] { null }));
        Assert.AreEqual("", (string)normalize.Invoke(ui, new object[] { "   " }));
    }

    /// <summary>
    /// Tester at ScrambleWords altid bevarer alle ord,
    /// bare i en blandet rækkefølge.
    /// </summary>
    [Test]
    public void ScrambleWords_PreservesWordMultiset()
    {
        var scramble = typeof(SpeechTaskUI)
            .GetMethod("ScrambleWords", PrivateInstance);
        Assert.IsNotNull(scramble);

        string sentence = "alpha bravo charlie delta";
        string scrambled = (string)scramble.Invoke(ui, new object[] { sentence });

        var originalWords = sentence.Split(' ').OrderBy(s => s).ToArray();
        var scrambledWords = scrambled.Split(' ').OrderBy(s => s).ToArray();

        CollectionAssert.AreEqual(originalWords, scrambledWords,
            "ScrambleWords skal bevare alle ord.");
    }

    /// <summary>
    /// Tester at ScrambleOneWord kun flytter ét ord
    /// og stadig bevarer hele ordlisten.
    /// </summary>
    [Test]
    public void ScrambleOneWord_PreservesWordMultiset()
    {
        var scramble = typeof(SpeechTaskUI)
            .GetMethod("ScrambleOneWord", PrivateInstance);
        Assert.IsNotNull(scramble);

        string sentence = "one two three four five";
        string scrambled = (string)scramble.Invoke(ui, new object[] { sentence });

        var originalWords = sentence.Split(' ').OrderBy(s => s).ToArray();
        var scrambledWords = scrambled.Split(' ').OrderBy(s => s).ToArray();

        CollectionAssert.AreEqual(originalWords, scrambledWords,
            "ScrambleOneWord skal bevare alle ord.");
    }

    /// <summary>
    /// Tester at Awake sætter panel, standard-tekster
    /// og skjuler UI korrekt.
    /// </summary>
    [Test]
    public void Awake_InitializesDefaultTextsAndHidesPanel()
    {
        // Nulstil panel, så Awake bruger gameObject som fallback
        typeof(SpeechTaskUI).GetField("panel", PrivateInstance)
            .SetValue(ui, null);

        var instrField = typeof(SpeechTaskUI).GetField("instructionText", PrivateInstance);
        var feedbackField = typeof(SpeechTaskUI).GetField("feedbackText", PrivateInstance);

        var instrGO = new GameObject("instrTmp");
        instrGO.transform.SetParent(go.transform, false);
        var instrComp = instrGO.AddComponent(instrField.FieldType);
        instrField.SetValue(ui, instrComp);

        var feedbackGO = new GameObject("fbTmp");
        feedbackGO.transform.SetParent(go.transform, false);
        var feedbackComp = feedbackGO.AddComponent(feedbackField.FieldType);
        feedbackField.SetValue(ui, feedbackComp);

        // Kald Awake
        var awake = typeof(SpeechTaskUI)
            .GetMethod("Awake", PrivateInstance);
        awake.Invoke(ui, null);

        var panel = (GameObject)typeof(SpeechTaskUI)
            .GetField("panel", PrivateInstance)
            .GetValue(ui);

        Assert.IsNotNull(panel);
        Assert.IsFalse(panel.activeSelf);
        Assert.AreEqual("Sig denne sætning:", GetTextFromPrivateField(ui, "instructionText"));
        Assert.AreEqual("", GetTextFromPrivateField(ui, "feedbackText"));

        Object.DestroyImmediate(instrGO);
        Object.DestroyImmediate(feedbackGO);
    }

    /// <summary>
    /// Tester at korrekt sagt sætning udløser OnTaskCompleted
    /// og viser succes-feedback i UI.
    /// </summary>
    [Test]
    public void OnSpeechRecognized_InvokesCompleted_OnExactMatch()
    {
        bool completed = false;
        ui.OnTaskCompleted = new UnityEvent();
        ui.OnTaskCompleted.AddListener(() => completed = true);

        var setupSentence = typeof(SpeechTaskUI)
            .GetMethod("SetupSentence", PrivateInstance);
        setupSentence.Invoke(ui, new object[] { "Hello World!" });

        ui.OnSpeechRecognized("hello world");

        var feedbackText = GetTextFromPrivateField(ui, "feedbackText");

        Assert.AreEqual("Rigtigt! 🐼", feedbackText);
        Assert.IsTrue(completed);
    }

    /// <summary>
    /// Tester at forkert sætning udløser OnTaskFailed
    /// og viser “jeg hørte...” feedback til barnet.
    /// </summary>
    [Test]
    public void OnSpeechRecognized_InvokesFailed_OnMismatch()
    {
        bool failed = false;
        ui.OnTaskFailed = new UnityEvent();
        ui.OnTaskFailed.AddListener(() => failed = true);

        var setupSentence = typeof(SpeechTaskUI)
            .GetMethod("SetupSentence", PrivateInstance);
        setupSentence.Invoke(ui, new object[] { "Jeg har set en pirat" });

        ui.OnSpeechRecognized("something else");

        var feedbackText = GetTextFromPrivateField(ui, "feedbackText");

        Assert.IsTrue(feedbackText.StartsWith("Jeg hørte:"),
            "UI skal vise hvad barnet faktisk sagde.");
        Assert.IsTrue(failed);
    }

    /// <summary>
    /// Tester Editor-simulationen af ShowTask:
    /// UI skal vise panel, simulere fejl-lytte-resultat,
    /// og kalde OnTaskFailed.
    /// </summary>
    [UnityTest]
    public IEnumerator ShowTask_InEditor_SimulatesFailureAndUpdatesUI()
    {
        bool failed = false;
        ui.OnTaskFailed = new UnityEvent();
        ui.OnTaskFailed.AddListener(() => failed = true);

        ui.ShowTask("En test sætning");

        yield return null; // giv UI en frame

        var panel = (GameObject)typeof(SpeechTaskUI)
            .GetField("panel", PrivateInstance)
            .GetValue(ui);

        Assert.IsTrue(panel.activeSelf, "Panelet skal være synligt efter ShowTask.");
        Assert.IsTrue(failed, "OnTaskFailed skal være kaldt i Editor-simulationen.");
    }

    /// <summary>
    /// Tester at StopListening stopper mikrofonen korrekt
    /// hos VoiceMovement.
    /// </summary>
    [Test]
    public void StopListening_WithVoiceMovement_StopsMicrophone()
    {
        var vmGO = new GameObject("VoiceMovement");
        vmGO.transform.SetParent(go.transform, false);
        var vm = vmGO.AddComponent<VoiceMovement>();

        typeof(SpeechTaskUI).GetField("voiceMovement", PrivateInstance)
            .SetValue(ui, vm);

        vm.isMicrophoneOn = true;

        var nestedType = typeof(SpeechTaskUI)
            .GetNestedType("TaskObserver", PrivateInstance);
        var ctor = nestedType.GetConstructor(new[] { typeof(SpeechTaskUI) });
        var taskObserverInstance = ctor.Invoke(new object[] { ui });

        typeof(SpeechTaskUI).GetField("_taskObserver", PrivateInstance)
            .SetValue(ui, taskObserverInstance);

        var stopListening = typeof(SpeechTaskUI)
            .GetMethod("StopListening", PrivateInstance);
        stopListening.Invoke(ui, null);

        Assert.IsFalse(vm.isMicrophoneOn, "StopListening skal slukke mikrofonen.");

        Object.DestroyImmediate(vmGO);
    }

    // Dummy observer (brugt ikke direkte, men rar at have hvis du senere vil teste observere)
    private class DummyObserver : IVoiceObserver
    {
        public void OnPartialResult(string partial) { }
        public void OnResult(string result) { }
        public void OnVoiceLevelChanged(float level) { }
        public void OnMicrophoneStateChanged(bool isOn) { }
    }
}
