using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Enhedstest for <see cref="MonsterHealthUI"/>.
/// Tester opdatering af UI-værdier, binding til et Monster-objekt og
/// start-/update-adfærd i forskellige scenarier.
/// </summary>
public class MonsterHealthUITest
{
    /// <summary>
    /// Henter et privat felt fra <see cref="MonsterHealthUI"/> via reflection.
    /// </summary>
    /// <param name="name">Navnet på det private felt der skal hentes.</param>
    /// <returns>Et <see cref="FieldInfo"/> for det fundne felt.</returns>
    private static FieldInfo GetPrivateField(string name) =>
        typeof(MonsterHealthUI).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);

    /// <summary>
    /// Henter en privat metode fra <see cref="MonsterHealthUI"/> via reflection.
    /// </summary>
    /// <param name="name">Navnet på den private metode der skal hentes.</param>
    /// <returns>Et <see cref="MethodInfo"/> for den fundne metode.</returns>
    private static MethodInfo GetPrivateMethod(string name) =>
        typeof(MonsterHealthUI).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance);

    /// <summary>
    /// Tester at <see cref="MonsterHealthUI.UpdateHealthUI(int,int)"/> klamper
    /// værdier korrekt og opdaterer både tekst og fyldningsgrad på søjlen.
    /// Dækker negative værdier og almindelige procentberegninger.
    /// </summary>
    [Test]
    public void UpdateHealthUI_ClampsValues_AndUpdatesTextAndBar()
    {
        var uiGO = new GameObject("UI");
        var ui = uiGO.AddComponent<MonsterHealthUI>();

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
    /// Tester at <see cref="MonsterHealthUI.SetMonster(Monster)"/> initialiserer UI korrekt
    /// ud fra et Monster-objekts nuværende og maksimale liv.
    /// </summary>
    [Test]
    public void SetMonster_UpdatesUIFromMonster()
    {
        var monsterGO = new GameObject("Monster");
        var monster = monsterGO.AddComponent<Dragon>();
        monster.MaxHealth = 80;
        monster.CurrentHealth = 25;

        var uiGO = new GameObject("UI");
        var ui = uiGO.AddComponent<MonsterHealthUI>();

        var tmpGO = new GameObject("TMP");
        var tmp = tmpGO.AddComponent<TextMeshProUGUI>();

        var barGO = new GameObject("Bar");
        var bar = barGO.AddComponent<Image>();
        bar.type = Image.Type.Filled;

        GetPrivateField("healthText").SetValue(ui, tmp);
        GetPrivateField("healthBar").SetValue(ui, bar);

        try
        {
            ui.SetMonster(monster);

            Assert.AreEqual("25 / 80", tmp.text);
            Assert.AreEqual(25f / 80f, bar.fillAmount, 1e-6f);
        }
        finally
        {
            Object.DestroyImmediate(monsterGO);
            Object.DestroyImmediate(uiGO);
            Object.DestroyImmediate(tmpGO);
            Object.DestroyImmediate(barGO);
        }
    }

    /// <summary>
    /// Tester at objektets GameObject aktiveres når et Monster er sat i Start,
    /// og at det forbliver deaktiveret hvis der ikke er noget Monster.
    /// </summary>
    [Test]
    public void Start_ActivatesWhenMonsterSet_AndDeactivatesWhenNot()
    {
        var monsterGO = new GameObject("Monster");
        var monster = monsterGO.AddComponent<Dragon>();
        monster.MaxHealth = 40;
        monster.CurrentHealth = 10;

        var uiGO = new GameObject("UI");
        var ui = uiGO.AddComponent<MonsterHealthUI>();

        var tmpGO = new GameObject("TMP");
        var tmp = tmpGO.AddComponent<TextMeshProUGUI>();

        var barGO = new GameObject("Bar");
        var bar = barGO.AddComponent<Image>();
        bar.type = Image.Type.Filled;

        GetPrivateField("healthText").SetValue(ui, tmp);
        GetPrivateField("healthBar").SetValue(ui, bar);
        GetPrivateField("monster").SetValue(ui, monster);

        try
        {
            GetPrivateMethod("Start").Invoke(ui, null);

            Assert.IsTrue(ui.gameObject.activeInHierarchy);
            Assert.AreEqual("10 / 40", tmp.text);
            Assert.AreEqual(10f / 40f, bar.fillAmount, 1e-6f);
        }
        finally
        {
            Object.DestroyImmediate(monsterGO);
            Object.DestroyImmediate(uiGO);
            Object.DestroyImmediate(tmpGO);
            Object.DestroyImmediate(barGO);
        }

        var uiGO2 = new GameObject("UI2");
        var ui2 = uiGO2.AddComponent<MonsterHealthUI>();

        try
        {
            GetPrivateMethod("Start").Invoke(ui2, null);
            Assert.IsFalse(ui2.gameObject.activeInHierarchy);
        }
        finally
        {
            Object.DestroyImmediate(uiGO2);
        }
    }

    /// <summary>
    /// Tester at <see cref="MonsterHealthUI"/> opdaterer UI når Monster's værdier ændres
    /// og at Update-metoden afspejler disse ændringer i tekst og fyldningsgrad.
    /// </summary>
    [Test]
    public void Update_UpdatesWhenMonsterValuesChange()
    {
        var monsterGO = new GameObject("Monster");
        var monster = monsterGO.AddComponent<Dragon>();
        monster.MaxHealth = 20;
        monster.CurrentHealth = 20;

        var uiGO = new GameObject("UI");
        var ui = uiGO.AddComponent<MonsterHealthUI>();

        var tmpGO = new GameObject("TMP");
        var tmp = tmpGO.AddComponent<TextMeshProUGUI>();

        var barGO = new GameObject("Bar");
        var bar = barGO.AddComponent<Image>();
        bar.type = Image.Type.Filled;

        GetPrivateField("healthText").SetValue(ui, tmp);
        GetPrivateField("healthBar").SetValue(ui, bar);

        try
        {
            ui.SetMonster(monster);
            Assert.AreEqual("20 / 20", tmp.text);
            Assert.AreEqual(1f, bar.fillAmount, 1e-6f);

            monster.CurrentHealth = 5;
            GetPrivateMethod("Update").Invoke(ui, null);

            Assert.AreEqual("5 / 20", tmp.text);
            Assert.AreEqual(5f / 20f, bar.fillAmount, 1e-6f);
        }
        finally
        {
            Object.DestroyImmediate(monsterGO);
            Object.DestroyImmediate(uiGO);
            Object.DestroyImmediate(tmpGO);
            Object.DestroyImmediate(barGO);
        }
    }
}
