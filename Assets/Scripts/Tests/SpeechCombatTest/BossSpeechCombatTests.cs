using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.TestTools;

/// <summary>
/// Tests af BossSpeechCombat – tjekker at bossen bruger
/// fuld-scramble når der er tekst, og normal opgave
/// når sætningen er tom.
/// </summary>
public class BossSpeechCombatTests
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
        var uiGO = new GameObject("SpeechUI_Boss");
        var ui = uiGO.AddComponent<SpeechTaskUI>();

        panelGO = new GameObject("Panel_Boss");
        panelGO.transform.SetParent(uiGO.transform, false);

        var instrGO = new GameObject("Instruction_Boss");
        instrGO.transform.SetParent(panelGO.transform, false);
        instructionText = instrGO.AddComponent<TextMeshProUGUI>();

        var sentenceGO = new GameObject("Sentence_Boss");
        sentenceGO.transform.SetParent(panelGO.transform, false);
        sentenceText = sentenceGO.AddComponent<TextMeshProUGUI>();

        var feedbackGO = new GameObject("Feedback_Boss");
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

        // IMPORTANT: Ensure UnityEvent fields are initialized so OnEnable/OnDisable can add/remove listeners safely.
        ui.OnTaskCompleted = new UnityEvent();
        ui.OnTaskFailed = new UnityEvent();

        return ui;
    }

    [UnityTest]
    public IEnumerator ShowSentenceWithCorrectMode_WithNonEmptySentence_UsesScrambledMode()
    {
        // Arrange
        var bossGO = new GameObject("Boss");
        bossGO.AddComponent<TestMonster>();
        var boss = bossGO.AddComponent<BossSpeechCombat>();
        Assert.IsNotNull(boss);

        var ui = CreateUI(
            "Jeg har set en pirat",
            out var panelGO,
            out var instrText,
            out var sentenceText,
            out var feedbackText);

        typeof(MonsterSpeechCombatBase).GetField("speechTaskUI",
                BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(boss, ui);

        var method = typeof(BossSpeechCombat).GetMethod(
            "ShowSentenceWithCorrectMode",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(method);

        const string sentence = "en test sætning";
        method.Invoke(boss, new object[] { sentence });

        // let Unity process the frame so UI activation / StartListening side-effects complete
        yield return null;

        // Assert – fuld-scramble-instruktion
        Assert.AreEqual("Sæt ordene i rigtig rækkefølge og sig sætningen:", instrText.text);
        Assert.IsTrue(panelGO.activeSelf);
        Assert.IsFalse(string.IsNullOrEmpty(sentenceText.text),
            "Sætningsteksten skal være udfyldt i scrambled-mode.");

        Object.DestroyImmediate(bossGO);
        Object.DestroyImmediate(ui.gameObject);
    }

    [UnityTest]
    public IEnumerator ShowSentenceWithCorrectMode_WithNullOrEmpty_FallsBackToNormalMode()
    {
        // Arrange
        var bossGO = new GameObject("Boss2");
        bossGO.AddComponent<TestMonster>();
        var boss = bossGO.AddComponent<BossSpeechCombat>();
        Assert.IsNotNull(boss);

        var ui = CreateUI(
            "Min default sætning",
            out var panelGO,
            out var instrText,
            out var sentenceText,
            out var feedbackText);

        typeof(MonsterSpeechCombatBase).GetField("speechTaskUI",
                BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(boss, ui);

        var method = typeof(BossSpeechCombat).GetMethod(
            "ShowSentenceWithCorrectMode",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(method);

        method.Invoke(boss, new object[] { null });

        // let Unity process the frame so UI activation / StartListening side-effects complete
        yield return null;

        // Assert – normal opgave med default-sætning
        Assert.AreEqual("Sig denne sætning:", instrText.text);
        Assert.IsTrue(panelGO.activeSelf);
        Assert.AreEqual("\"Min default sætning\"", sentenceText.text);

        Object.DestroyImmediate(bossGO);
        Object.DestroyImmediate(ui.gameObject);
    }
}