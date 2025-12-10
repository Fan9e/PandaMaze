using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Tests af DragonSpeechCombat – tjekker at den viser
/// normale “sig sætningen”-opgaver korrekt.
/// </summary>
public class DragonSpeechCombatTest
{
    /// <summary>
    /// Lille konkret Monster-klasse til test, så vi ikke er afhængige
    /// af den rigtige Monster-implementation.
    /// </summary>
    private class TestMonster : Monster
    {
        // Tom – vi bruger Monsters egen logik.
    }

    /// <summary>
    /// Hjælpe-metode der opretter et SpeechTaskUI-objekt med TMP-felter
    /// og injicerer dem via reflection, så testen kan bruge UI’et.
    /// </summary>
    private SpeechTaskUI CreateUI(
        string defaultSentence,
        out GameObject panelGO,
        out TextMeshProUGUI instructionText,
        out TextMeshProUGUI sentenceText,
        out TextMeshProUGUI feedbackText)
    {
        var uiGO = new GameObject("SpeechUI");
        var ui = uiGO.AddComponent<SpeechTaskUI>();

        // Panel
        panelGO = new GameObject("Panel");
        panelGO.transform.SetParent(uiGO.transform, false);

        // Instruction
        var instrGO = new GameObject("Instruction");
        instrGO.transform.SetParent(panelGO.transform, false);
        instructionText = instrGO.AddComponent<TextMeshProUGUI>();

        // Sentence
        var sentenceGO = new GameObject("Sentence");
        sentenceGO.transform.SetParent(panelGO.transform, false);
        sentenceText = sentenceGO.AddComponent<TextMeshProUGUI>();

        // Feedback
        var feedbackGO = new GameObject("Feedback");
        feedbackGO.transform.SetParent(panelGO.transform, false);
        feedbackText = feedbackGO.AddComponent<TextMeshProUGUI>();

        // Injicer felter på SpeechTaskUI
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

    [Test]
    public void ShowSentenceWithCorrectMode_WithNonEmptySentence_ShowsNormalTask()
    {
        // Arrange
        var dragonGO = new GameObject("Dragon");
        dragonGO.AddComponent<TestMonster>();                       // opfylder RequireComponent(Monster)
        var dragon = dragonGO.AddComponent<DragonSpeechCombat>();
        Assert.IsNotNull(dragon, "Kunne ikke oprette DragonSpeechCombat.");

        var ui = CreateUI(
            "Jeg har set en pirat",
            out var panelGO,
            out var instrText,
            out var sentenceText,
            out var feedbackText);

        // injicer speechTaskUI i base-klassen
        typeof(MonsterSpeechCombatBase).GetField("speechTaskUI",
                BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(dragon, ui);

        // Act – kald den beskyttede metode via reflection
        var method = typeof(DragonSpeechCombat).GetMethod(
            "ShowSentenceWithCorrectMode",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(method, "Kunne ikke finde ShowSentenceWithCorrectMode via reflection.");

        const string sentence = "Sagde dragen noget";
        method.Invoke(dragon, new object[] { sentence });

        // Assert – normal opgave: standard-instruktion og citeret tekst
        Assert.AreEqual("Sig denne sætning:", instrText.text);
        Assert.IsTrue(panelGO.activeSelf, "Panelet skal være synligt efter opgaven vises.");
        Assert.AreEqual($"\"{sentence}\"", sentenceText.text);

        Object.DestroyImmediate(dragonGO);
        Object.DestroyImmediate(ui.gameObject);
    }

    [Test]
    public void ShowSentenceWithCorrectMode_WithNull_UsesDefaultSentence()
    {
        // Arrange
        var dragonGO = new GameObject("Dragon2");
        dragonGO.AddComponent<TestMonster>();
        var dragon = dragonGO.AddComponent<DragonSpeechCombat>();
        Assert.IsNotNull(dragon, "Kunne ikke oprette DragonSpeechCombat.");

        var ui = CreateUI(
            "Min default sætning",
            out var panelGO,
            out var instrText,
            out var sentenceText,
            out var feedbackText);

        typeof(MonsterSpeechCombatBase).GetField("speechTaskUI",
                BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(dragon, ui);

        // Act – null -> SpeechTaskUI skal bruge defaultSentence
        var method = typeof(DragonSpeechCombat).GetMethod(
            "ShowSentenceWithCorrectMode",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(method);

        method.Invoke(dragon, new object[] { null });

        // Assert – normalinstruktion + citeret default-sætning
        Assert.AreEqual("Sig denne sætning:", instrText.text);
        Assert.IsTrue(panelGO.activeSelf);
        Assert.AreEqual("\"Min default sætning\"", sentenceText.text);

        Object.DestroyImmediate(dragonGO);
        Object.DestroyImmediate(ui.gameObject);
    }
}