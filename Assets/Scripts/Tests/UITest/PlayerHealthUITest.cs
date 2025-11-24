using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Enhedstest for `PlayerHealthUI`.
/// Indeholder tests der verificerer at UI-komponenterne for spillerens liv (tekst og fyldbar) opdateres korrekt
/// under forskellige forhold (klamping, initiering fra scene, opdatering ved ændringer).
/// </summary>
public class PlayerHealthUITest : UnityTestBase
{
    /// <summary>
    /// Henter et privat felt fra `PlayerHealthUI` via reflection.
    /// </summary>
    /// <param name="name">Navnet på det private felt.</param>
    /// <returns>Det fundne <see cref="FieldInfo"/>-objekt.</returns>
    private static FieldInfo GetPrivateField(string name) =>
        typeof(PlayerHealthUI).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
        
    /// <summary>
    /// Henter en privat metode fra `PlayerHealthUI` via reflection.
    /// </summary>
    /// <param name="name">Navnet på den private metode.</param>
    /// <returns>Det fundne <see cref="MethodInfo"/>-objekt.</returns>
    private static MethodInfo GetPrivateMethod(string name) =>
        typeof(PlayerHealthUI).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance);

    /// <summary>
    /// Verificerer at `UpdateHealthUI` klamper værdier til gyldige intervaller og opdaterer både tekst og fyldbar korrekt.
    /// Testen dækker både underflow (negativt aktuelle liv) og proportionel opdatering.
    /// </summary>
    [Test]
    public void UpdateHealthUI_ClampsValues_AndUpdatesTextAndBar()
    {
        var uiGO = CreateGameObject("UI");
        var ui = uiGO.AddComponent<PlayerHealthUI>();

        var textGO = CreateGameObject("TMP");
        var tmp = textGO.AddComponent<TextMeshProUGUI>();

        var barGO = CreateGameObject("Bar");
        var bar = barGO.AddComponent<Image>();
        bar.type = Image.Type.Filled;

        GetPrivateField("healthText").SetValue(ui, tmp);
        GetPrivateField("healthBar").SetValue(ui, bar);

        ui.UpdateHealthUI(-5, 0);
        Assert.AreEqual("0 / 1", tmp.text);
        Assert.AreEqual(0f, bar.fillAmount, 1e-6f);

        ui.UpdateHealthUI(50, 100);
        Assert.AreEqual("50 / 100", tmp.text);
        Assert.AreEqual(0.5f, bar.fillAmount, 1e-6f);
    }

    /// <summary>
    /// Verificerer at `SetPlayer` opdaterer UI-komponenterne ud fra spillerens aktuelle værdier.
    /// </summary>
    [Test]
    public void SetPlayer_UpdatesUIFromPlayer()
    {
        var playerGO = CreateGameObject("Player");
        var player = playerGO.AddComponent<Player>();
        player.MaxHealth = 80;
        player.CurrentHealth = 25;

        var uiGO = CreateGameObject("UI");
        var ui = uiGO.AddComponent<PlayerHealthUI>();

        var tmpGO = CreateGameObject("TMP");
        var tmp = tmpGO.AddComponent<TextMeshProUGUI>();

        var barGO = CreateGameObject("Bar");
        var bar = barGO.AddComponent<Image>();
        bar.type = Image.Type.Filled;

        GetPrivateField("healthText").SetValue(ui, tmp);
        GetPrivateField("healthBar").SetValue(ui, bar);

        ui.SetPlayer(player);

        Assert.AreEqual("25 / 80", tmp.text);
        Assert.AreEqual(25f / 80f, bar.fillAmount, 1e-6f);
    }

    /// <summary>
    /// Verificerer at `Start` finder en `Player` i scenen, hvis UI'en ikke allerede har en spiller tilknyttet,
    /// og derefter initialiserer UI'en med spillerens værdier.
    /// </summary>
    [Test]
    public void Start_FindsPlayerInScene_WhenPlayerNotAssigned()
    {
        var playerGO = CreateGameObject("ScenePlayer");
        var player = playerGO.AddComponent<Player>();
        player.MaxHealth = 40;
        player.CurrentHealth = 10;

        var uiGO = CreateGameObject("UI");
        var ui = uiGO.AddComponent<PlayerHealthUI>();

        var tmpGO = CreateGameObject("TMP");
        var tmp = tmpGO.AddComponent<TextMeshProUGUI>();

        var barGO = CreateGameObject("Bar");
        var bar = barGO.AddComponent<Image>();
        bar.type = Image.Type.Filled;

        GetPrivateField("healthText").SetValue(ui, tmp);
        GetPrivateField("healthBar").SetValue(ui, bar);

        GetPrivateMethod("Start").Invoke(ui, null);

        Assert.AreEqual("10 / 40", tmp.text);
        Assert.AreEqual(10f / 40f, bar.fillAmount, 1e-6f);
    }

    /// <summary>
    /// Verificerer at UI'en opdateres i `Update`, når spillerens liv ændres mellem frames.
    /// </summary>
    [Test]
    public void Update_UpdatesWhenPlayerValuesChange()
    {
        var playerGO = CreateGameObject("Player");
        var player = playerGO.AddComponent<Player>();
        player.MaxHealth = 20;
        player.CurrentHealth = 20;

        var uiGO = CreateGameObject("UI");
        var ui = uiGO.AddComponent<PlayerHealthUI>();

        var tmpGO = CreateGameObject("TMP");
        var tmp = tmpGO.AddComponent<TextMeshProUGUI>();

        var barGO = CreateGameObject("Bar");
        var bar = barGO.AddComponent<Image>();
        bar.type = Image.Type.Filled;

        GetPrivateField("healthText").SetValue(ui, tmp);
        GetPrivateField("healthBar").SetValue(ui, bar);

        ui.SetPlayer(player);
        Assert.AreEqual("20 / 20", tmp.text);
        Assert.AreEqual(1f, bar.fillAmount, 1e-6f);

        player.CurrentHealth = 5;
        GetPrivateMethod("Update").Invoke(ui, null);

        Assert.AreEqual("5 / 20", tmp.text);
        Assert.AreEqual(5f / 20f, bar.fillAmount, 1e-6f);
    }
}