using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Enhedstest for <see cref="MonsterHealthUI"/>.
/// Indeholder tests der verificerer at UI-komponenterne for monstrenes liv (tekst og fyldbar) opdateres korrekt
/// under forskellige forhold (klamping, initiering, aktivering/deaktivering og opdatering mellem frames).
/// </summary>
public class MonsterHealthUITest : UnityTestBase
{
    /// <summary>
    /// Henter et privat felt fra <see cref="MonsterHealthUI"/> via reflection.
    /// </summary>
    /// <param name="name">Navnet på det private felt der skal hentes.</param>
    /// <returns>Et <see cref="FieldInfo"/> for det fundne private felt.</returns>
    private static FieldInfo GetPrivateField(string name) =>
        typeof(MonsterHealthUI).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);

    /// <summary>
    /// Henter en privat metode fra <see cref="MonsterHealthUI"/> via reflection.
    /// </summary>
    /// <param name="name">Navnet på den private metode der skal hentes.</param>
    /// <returns>Et <see cref="MethodInfo"/> for den fundne private metode.</returns>
    private static MethodInfo GetPrivateMethod(string name) =>
        typeof(MonsterHealthUI).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance);

    /// <summary>
    /// Verificerer at <see cref="MonsterHealthUI.UpdateHealthUI"/> klamper værdier til gyldige intervaller
    /// og opdaterer både tekst og fyldbar korrekt.
    /// Testen dækker underflow (negativt aktuelle liv) og proportional opdatering ved normale værdier.
    /// </summary>
    [Test]
    public void UpdateHealthUI_ClampsValues_AndUpdatesTextAndBar()
    {
        var uiGO = CreateGameObject("UI");
        var ui = uiGO.AddComponent<MonsterHealthUI>();

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
    /// Verificerer at <see cref="MonsterHealthUI.SetMonster"/> opdaterer UI-komponenterne ud fra monsterets aktuelle værdier.
    /// </summary>
    [Test]
    public void SetMonster_UpdatesUIFromMonster()
    {
        var monsterGO = CreateGameObject("Monster");
        var monster = monsterGO.AddComponent<Dragon>();
        monster.MaxHealth = 80;
        monster.CurrentHealth = 25;

        var uiGO = CreateGameObject("UI");
        var ui = uiGO.AddComponent<MonsterHealthUI>();

        var tmpGO = CreateGameObject("TMP");
        var tmp = tmpGO.AddComponent<TextMeshProUGUI>();

        var barGO = CreateGameObject("Bar");
        var bar = barGO.AddComponent<Image>();
        bar.type = Image.Type.Filled;

        GetPrivateField("healthText").SetValue(ui, tmp);
        GetPrivateField("healthBar").SetValue(ui, bar);

        ui.SetMonster(monster);

        Assert.AreEqual("25 / 80", tmp.text);
        Assert.AreEqual(25f / 80f, bar.fillAmount, 1e-6f);
    }

    /// <summary>
    /// Verificerer at <see cref="MonsterHealthUI.Start"/> aktiverer UI'en når et monster er sat,
    /// initialiserer tekst og bar med monsterets værdier, og deaktiverer UI'en når ingen monster er tildelt.
    /// </summary>
    [Test]
    public void Start_ActivatesWhenMonsterSet_AndDeactivatesWhenNot()
    {
        var monsterGO = CreateGameObject("Monster");
        var monster = monsterGO.AddComponent<Dragon>();
        monster.MaxHealth = 40;
        monster.CurrentHealth = 10;

        var uiGO = CreateGameObject("UI");
        var ui = uiGO.AddComponent<MonsterHealthUI>();

        var tmpGO = CreateGameObject("TMP");
        var tmp = tmpGO.AddComponent<TextMeshProUGUI>();

        var barGO = CreateGameObject("Bar");
        var bar = barGO.AddComponent<Image>();
        bar.type = Image.Type.Filled;

        GetPrivateField("healthText").SetValue(ui, tmp);
        GetPrivateField("healthBar").SetValue(ui, bar);
        GetPrivateField("monster").SetValue(ui, monster);

        GetPrivateMethod("Start").Invoke(ui, null);

        Assert.IsTrue(ui.gameObject.activeInHierarchy);
        Assert.AreEqual("10 / 40", tmp.text);
        Assert.AreEqual(10f / 40f, bar.fillAmount, 1e-6f);

        var uiGO2 = CreateGameObject("UI2");
        var ui2 = uiGO2.AddComponent<MonsterHealthUI>();

        GetPrivateMethod("Start").Invoke(ui2, null);
        Assert.IsFalse(ui2.gameObject.activeInHierarchy);
    }

    /// <summary>
    /// Verificerer at UI'en opdateres i <see cref="MonsterHealthUI.Update"/>, når monsterets liv ændres mellem frames.
    /// </summary>
    [Test]
    public void Update_UpdatesWhenMonsterValuesChange()
    {
        var monsterGO = CreateGameObject("Monster");
        var monster = monsterGO.AddComponent<Dragon>();
        monster.MaxHealth = 20;
        monster.CurrentHealth = 20;

        var uiGO = CreateGameObject("UI");
        var ui = uiGO.AddComponent<MonsterHealthUI>();

        var tmpGO = CreateGameObject("TMP");
        var tmp = tmpGO.AddComponent<TextMeshProUGUI>();

        var barGO = CreateGameObject("Bar");
        var bar = barGO.AddComponent<Image>();
        bar.type = Image.Type.Filled;

        GetPrivateField("healthText").SetValue(ui, tmp);
        GetPrivateField("healthBar").SetValue(ui, bar);

        ui.SetMonster(monster);
        Assert.AreEqual("20 / 20", tmp.text);
        Assert.AreEqual(1f, bar.fillAmount, 1e-6f);

        monster.CurrentHealth = 5;
        GetPrivateMethod("Update").Invoke(ui, null);

        Assert.AreEqual("5 / 20", tmp.text);
        Assert.AreEqual(5f / 20f, bar.fillAmount, 1e-6f);
    }
}
