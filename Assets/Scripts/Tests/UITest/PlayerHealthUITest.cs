using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Enhedstestklasse for <see cref="PlayerHealthUI"/>.
/// </summary>
/// <remarks>
/// Indeholder tests der verificerer at UI'et opdateres korrekt når spillerens helbred ændres,
/// at UI kan sættes fra en <see cref="Player"/>-instans, og at <c>Start</c> finder spilleren i scenen
/// hvis den ikke allerede er sat.
/// </remarks>
public class PlayerHealthUITest
{
    /// <summary>
    /// Henter et privat felt fra <see cref="PlayerHealthUI"/> via reflection.
    /// </summary>
    /// <param name="name">Navnet på det private felt.</param>
    /// <returns>Et <see cref="FieldInfo"/> for det ønskede felt.</returns>
    private static FieldInfo GetPrivateField(string name) =>
        typeof(PlayerHealthUI).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);

    /// <summary>
    /// Henter en privat metode fra <see cref="PlayerHealthUI"/> via reflection.
    /// </summary>
    /// <param name="name">Navnet på den private metode.</param>
    /// <returns>Et <see cref="MethodInfo"/> for den ønskede metode.</returns>
    private static MethodInfo GetPrivateMethod(string name) =>
        typeof(PlayerHealthUI).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance);

    /// <summary>
    /// Tester at <see cref="PlayerHealthUI.UpdateHealthUI"/> klemmer værdier indenfor gyldigt interval
    /// og opdaterer både tekst og fyldningsbar korrekt.
    /// </summary>
    [Test]
    public void UpdateHealthUI_ClampsValues_AndUpdatesTextAndBar()
    {
        var uiGO = new GameObject("UI");
        var ui = uiGO.AddComponent<PlayerHealthUI>();

        var textGO = new GameObject("TMP");
        var tmp = textGO.AddComponent<TextMeshProUGUI>();

        var barGO = new GameObject("Bar");
        var bar = barGO.AddComponent<Image>();
        bar.type = Image.Type.Filled;

        GetPrivateField("healthText").SetValue(ui, tmp);
        GetPrivateField("healthBar").SetValue(ui, bar);

        try
        {
            ui.UpdateHealthUI(-5, 0);
            Assert.AreEqual("0 / 1", tmp.text);
            Assert.AreEqual(0f, bar.fillAmount, 1e-6f);

            ui.UpdateHealthUI(50, 100);
            Assert.AreEqual("50 / 100", tmp.text);
            Assert.AreEqual(0.5f, bar.fillAmount, 1e-6f);
        }
        finally
        {
            Object.DestroyImmediate(uiGO);
            Object.DestroyImmediate(textGO);
            Object.DestroyImmediate(barGO);
        }
    }

    /// <summary>
    /// Tester at <see cref="PlayerHealthUI.SetPlayer"/> opdaterer UI'et ud fra en given <see cref="Player"/>.
    /// </summary>
    [Test]
    public void SetPlayer_UpdatesUIFromPlayer()
    {
        var playerGO = new GameObject("Player");
        var player = playerGO.AddComponent<Player>();
        player.MaxHealth = 80;
        player.CurrentHealth = 25;

        var uiGO = new GameObject("UI");
        var ui = uiGO.AddComponent<PlayerHealthUI>();

        var tmpGO = new GameObject("TMP");
        var tmp = tmpGO.AddComponent<TextMeshProUGUI>();

        var barGO = new GameObject("Bar");
        var bar = barGO.AddComponent<Image>();
        bar.type = Image.Type.Filled;

        GetPrivateField("healthText").SetValue(ui, tmp);
        GetPrivateField("healthBar").SetValue(ui, bar);

        try
        {
            ui.SetPlayer(player);

            Assert.AreEqual("25 / 80", tmp.text);
            Assert.AreEqual(25f / 80f, bar.fillAmount, 1e-6f);
        }
        finally
        {
            Object.DestroyImmediate(playerGO);
            Object.DestroyImmediate(uiGO);
            Object.DestroyImmediate(tmpGO);
            Object.DestroyImmediate(barGO);
        }
    }

    /// <summary>
    /// Tester at <c>Start</c> finder en <see cref="Player"/> i scenen og initialiserer UI korrekt,
    /// når der ikke er sat en spiller på forhånd.
    /// </summary>
    [Test]
    public void Start_FindsPlayerInScene_WhenPlayerNotAssigned()
    {
        var playerGO = new GameObject("ScenePlayer");
        var player = playerGO.AddComponent<Player>();
        player.MaxHealth = 40;
        player.CurrentHealth = 10;

        var uiGO = new GameObject("UI");
        var ui = uiGO.AddComponent<PlayerHealthUI>();

        var tmpGO = new GameObject("TMP");
        var tmp = tmpGO.AddComponent<TextMeshProUGUI>();

        var barGO = new GameObject("Bar");
        var bar = barGO.AddComponent<Image>();
        bar.type = Image.Type.Filled;

        GetPrivateField("healthText").SetValue(ui, tmp);
        GetPrivateField("healthBar").SetValue(ui, bar);

        try
        {
            GetPrivateMethod("Start").Invoke(ui, null);

            Assert.AreEqual("10 / 40", tmp.text);
            Assert.AreEqual(10f / 40f, bar.fillAmount, 1e-6f);
        }
        finally
        {
            Object.DestroyImmediate(playerGO);
            Object.DestroyImmediate(uiGO);
            Object.DestroyImmediate(tmpGO);
            Object.DestroyImmediate(barGO);
        }
    }

    /// <summary>
    /// Tester at <see cref="PlayerHealthUI.Update"/> opdaterer UI'et når spillerens værdier ændres mellem frames.
    /// </summary>
    [Test]
    public void Update_UpdatesWhenPlayerValuesChange()
    {
        var playerGO = new GameObject("Player");
        var player = playerGO.AddComponent<Player>();
        player.MaxHealth = 20;
        player.CurrentHealth = 20;

        var uiGO = new GameObject("UI");
        var ui = uiGO.AddComponent<PlayerHealthUI>();

        var tmpGO = new GameObject("TMP");
        var tmp = tmpGO.AddComponent<TextMeshProUGUI>();

        var barGO = new GameObject("Bar");
        var bar = barGO.AddComponent<Image>();
        bar.type = Image.Type.Filled;

        GetPrivateField("healthText").SetValue(ui, tmp);
        GetPrivateField("healthBar").SetValue(ui, bar);

        try
        {
            ui.SetPlayer(player);
            Assert.AreEqual("20 / 20", tmp.text);
            Assert.AreEqual(1f, bar.fillAmount, 1e-6f);

            player.CurrentHealth = 5;
            GetPrivateMethod("Update").Invoke(ui, null);

            Assert.AreEqual("5 / 20", tmp.text);
            Assert.AreEqual(5f / 20f, bar.fillAmount, 1e-6f);
        }
        finally
        {
            Object.DestroyImmediate(playerGO);
            Object.DestroyImmediate(uiGO);
            Object.DestroyImmediate(tmpGO);
            Object.DestroyImmediate(barGO);
        }
    }
}