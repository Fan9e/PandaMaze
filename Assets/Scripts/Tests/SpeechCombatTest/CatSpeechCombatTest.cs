using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.TestTools;

/// <summary>
/// Tests af CatSpeechCombat – tjekker at katten bruger
/// one-word-scrambled når der er en sætning, og normal
/// opgave når sætningen er tom.
/// </summary>
public class CatSpeechCombatTest
{
    private class TestMonster : Monster
    {
    }

    private SpeechTaskUI CreateUI(
        string defaultSentence,
        out GameObject panelGO,
        out TextMeshProUGUI instructionText,
        out TextMeshProUGUI sentenceText,
        out TextMeshProUGUI feedbackText)
    {
        var uiGO = new GameObject("SpeechUI_Cat");
        var ui = uiGO.AddComponent<SpeechTaskUI>();

        panelGO = new GameObject("Panel_Cat");
        panelGO.transform.SetParent(uiGO.transform, false);

        var instrGO = new GameObject("Instruction_Cat");
        instrGO.transform.SetParent(panelGO.transform, false);
        instructionText = instrGO.AddComponent<TextMeshProUGUI>();

        var sentenceGO = new GameObject("Sentence_Cat");
        sentenceGO.transform.SetParent(panelGO.transform, false);
        sentenceText = sentenceGO.AddComponent<TextMeshProUGUI>();

        var feedbackGO = new GameObject("Feedback_Cat");
        feedbackGO.transform.SetParent(panelGO.transform, false);
        feedbackText = feedbackGO.AddComponent<TextMeshProUGUI>();

        typeof(SpeechTaskUI).GetField("panel", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(ui, panelGO);
        typeof(SpeechTaskUI).GetField("instructionText", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(ui, instructionText);
        typeof(SpeechTaskUI).GetField("sentenceText", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(ui, sentenceText);
        typeof(SpeechTaskUI).GetField("feedbackText", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(ui, feedbackText);
        typeof(SpeechTaskUI).GetField("defaultSentence", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(ui, defaultSentence);

        // Ensure UnityEvent fields exist so OnEnable/OnDisable can safely add/remove listeners.
        ui.OnTaskCompleted = new UnityEvent();
        ui.OnTaskFailed = new UnityEvent();

        return ui;
    }

    [UnityTest]
    public IEnumerator ShowSentenceWithCorrectMode_WithNonEmptySentence_UsesOneWordScrambledMode()
    {
        // Arrange
        var catGO = new GameObject("Cat");
        catGO.AddComponent<TestMonster>();
        var cat = catGO.AddComponent<CatSpeechCombat>();
        Assert.IsNotNull(cat);

        var ui = CreateUI(
            "Jeg har set en pirat",
            out var panelGO,
            out var instrText,
            out var sentenceText,
            out var feedbackText);

        typeof(MonsterSpeechCombatBase).GetField("speechTaskUI",
                BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(cat, ui);

        var method = typeof(CatSpeechCombat).GetMethod(
            "ShowSentenceWithCorrectMode",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(method);

        const string sentence = "en test sætning";
        method.Invoke(cat, new object[] { sentence });

        // allow Unity to process frame (StartListening may run / simulate)
        yield return null;

        // Assert – ét ord forkert-mode
        Assert.AreEqual("Ét ord står forkert. Sig sætningen korrekt:", instrText.text);
        Assert.IsTrue(panelGO.activeSelf);
        Assert.IsFalse(string.IsNullOrEmpty(sentenceText.text),
            "Sætningsteksten skal være udfyldt i one-word-scrambled mode.");

        Object.DestroyImmediate(catGO);
        Object.DestroyImmediate(ui.gameObject);
    }

    [UnityTest]
    public IEnumerator ShowSentenceWithCorrectMode_WithNullOrEmpty_FallsBackToNormalMode()
    {
        // Arrange
        var catGO = new GameObject("Cat2");
        catGO.AddComponent<TestMonster>();
        var cat = catGO.AddComponent<CatSpeechCombat>();
        Assert.IsNotNull(cat);

        var ui = CreateUI(
            "Min default sætning",
            out var panelGO,
            out var instrText,
            out var sentenceText,
            out var feedbackText);

        typeof(MonsterSpeechCombatBase).GetField("speechTaskUI",
                BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(cat, ui);

        var method = typeof(CatSpeechCombat).GetMethod(
            "ShowSentenceWithCorrectMode",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(method);

        // null -> fald tilbage til normal ShowTask
        method.Invoke(cat, new object[] { null });

        // allow Unity to process frame (StartListening may run / simulate)
        yield return null;

        Assert.AreEqual("Sig denne sætning:", instrText.text);
        Assert.IsTrue(panelGO.activeSelf);
        Assert.AreEqual("\"Min default sætning\"", sentenceText.text);

        Object.DestroyImmediate(catGO);
        Object.DestroyImmediate(ui.gameObject);
    }
}