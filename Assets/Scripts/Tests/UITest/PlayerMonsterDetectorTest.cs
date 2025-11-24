using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Enhedstest for <see cref="PlayerMonsterDetector"/>.
/// Indeholder tests der verificerer at detektor-komponenten finder spiller og monster-UI i scenen,
/// samt at den viser og skjuler monster-UI korrekt baseret på afstand til nærmeste monster.
/// </summary>
public class PlayerMonsterDetectorTest : UnityTestBase
{
    /// <summary>
    /// Henter et privat felt fra <see cref="PlayerMonsterDetector"/> via reflection.
    /// </summary>
    /// <param name="name">Navnet på det private felt der skal hentes.</param>
    /// <returns>Et <see cref="FieldInfo"/> for det fundne private felt.</returns>
    private static FieldInfo GetPrivateField(string name) =>
        typeof(PlayerMonsterDetector).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);

    /// <summary>
    /// Henter en privat metode fra <see cref="PlayerMonsterDetector"/> via reflection.
    /// </summary>
    /// <param name="name">Navnet på den private metode der skal hentes.</param>
    /// <returns>Et <see cref="MethodInfo"/> for den fundne private metode.</returns>
    private static MethodInfo GetPrivateMethod(string name) =>
        typeof(PlayerMonsterDetector).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance);

    /// <summary>
    /// Verificerer at <see cref="PlayerMonsterDetector.Start"/> finder en spiller og en <see cref="MonsterHealthUI"/> i scenen
    /// og at UI'et bliver skjult ved initiering.
    /// </summary>
    [Test]
    public void Start_FindsPlayerAndMonsterHealthUI_AndHidesUI()
    {
        var playerGO = CreateGameObject("ScenePlayer");
        var player = playerGO.AddComponent<Player>();

        var uiGO = CreateGameObject("MonsterUI");
        var monsterUI = uiGO.AddComponent<MonsterHealthUI>();

        var tmpGO = CreateGameObject("TMP");
        tmpGO.transform.SetParent(uiGO.transform);
        var tmp = tmpGO.AddComponent<TextMeshProUGUI>();
        var barGO = CreateGameObject("Bar");
        barGO.transform.SetParent(uiGO.transform);
        var bar = barGO.AddComponent<Image>();
        bar.type = Image.Type.Filled;

        var detectorGO = CreateGameObject("Detector");
        var detector = detectorGO.AddComponent<PlayerMonsterDetector>();

        GetPrivateMethod("Start").Invoke(detector, null);

        var foundPlayer = (Player)GetPrivateField("player").GetValue(detector);
        var foundUI = (MonsterHealthUI)GetPrivateField("monsterHealthUI").GetValue(detector);

        Assert.IsNotNull(foundPlayer);
        Assert.IsNotNull(foundUI);
        Assert.IsFalse(monsterUI.gameObject.activeInHierarchy);
    }

    /// <summary>
    /// Verificerer at <see cref="PlayerMonsterDetector.Update"/> viser UI'et og binder det nærmeste monster,
    /// når et monster er indenfor detektionsradiusen.
    /// </summary>
    [Test]
    public void Update_ShowsNearestMonster_WhenWithinRange()
    {
        var playerGO = CreateGameObject("Player");
        playerGO.transform.position = Vector3.zero;
        var player = playerGO.AddComponent<Player>();

        var m1GO = CreateGameObject("MonsterNear");
        m1GO.transform.position = new Vector3(2f, 0f, 0f);
        var m1 = m1GO.AddComponent<Dragon>();

        var m2GO = CreateGameObject("MonsterFar");
        m2GO.transform.position = new Vector3(4f, 0f, 0f);
        var m2 = m2GO.AddComponent<Dragon>();

        var uiGO = CreateGameObject("MonsterUI");
        var monsterUI = uiGO.AddComponent<MonsterHealthUI>();
        var tmpGO = CreateGameObject("TMP");
        tmpGO.transform.SetParent(uiGO.transform);
        var tmp = tmpGO.AddComponent<TextMeshProUGUI>();
        var barGO = CreateGameObject("Bar");
        barGO.transform.SetParent(uiGO.transform);
        var bar = barGO.AddComponent<Image>();
        bar.type = Image.Type.Filled;

        var detectorGO = CreateGameObject("Detector");
        var detector = detectorGO.AddComponent<PlayerMonsterDetector>();

        GetPrivateField("player").SetValue(detector, player);
        GetPrivateField("monsterHealthUI").SetValue(detector, monsterUI);

        GetPrivateField("_timer").SetValue(detector, 0f);

        GetPrivateMethod("Update").Invoke(detector, null);

        Assert.IsTrue(monsterUI.gameObject.activeInHierarchy);
        var boundMonster = (Monster)typeof(MonsterHealthUI)
            .GetField("monster", BindingFlags.NonPublic | BindingFlags.Instance)
            .GetValue(monsterUI);
        Assert.AreEqual(m1, boundMonster);
    }

    /// <summary>
    /// Verificerer at <see cref="PlayerMonsterDetector.Update"/> skjuler UI'et og rydder den bundne monster-reference,
    /// når der ikke er nogen monstre indenfor detektionsradiusen.
    /// </summary>
    [Test]
    public void Update_HidesAndClears_WhenNoMonsterNearby()
    {
        var playerGO = CreateGameObject("Player");
        playerGO.transform.position = Vector3.zero;
        var player = playerGO.AddComponent<Player>();

        var mGO = CreateGameObject("MonsterFarAway");
        mGO.transform.position = new Vector3(100f, 0f, 0f);
        var monster = mGO.AddComponent<Dragon>();

        var uiGO = CreateGameObject("MonsterUI");
        var monsterUI = uiGO.AddComponent<MonsterHealthUI>();
        var tmpGO = CreateGameObject("TMP");
        tmpGO.transform.SetParent(uiGO.transform);
        tmpGO.AddComponent<TextMeshProUGUI>();
        var barGO = CreateGameObject("Bar");
        barGO.transform.SetParent(uiGO.transform);
        var bar = barGO.AddComponent<Image>();
        bar.type = Image.Type.Filled;

        var detectorGO = CreateGameObject("Detector");
        var detector = detectorGO.AddComponent<PlayerMonsterDetector>();

        GetPrivateField("player").SetValue(detector, player);
        GetPrivateField("monsterHealthUI").SetValue(detector, monsterUI);

        GetPrivateField("_timer").SetValue(detector, 0f);

        GetPrivateMethod("Update").Invoke(detector, null);

        Assert.IsFalse(monsterUI.gameObject.activeInHierarchy);

        var boundMonster = (Monster)typeof(MonsterHealthUI)
            .GetField("monster", BindingFlags.NonPublic | BindingFlags.Instance)
            .GetValue(monsterUI);
        Assert.IsNull(boundMonster);
    }
}
