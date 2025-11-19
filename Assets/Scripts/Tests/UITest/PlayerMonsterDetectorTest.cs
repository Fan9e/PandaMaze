using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerMonsterDetectorTest
{
    private static FieldInfo GetPrivateField(string name) =>
        typeof(PlayerMonsterDetector).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);

    private static MethodInfo GetPrivateMethod(string name) =>
        typeof(PlayerMonsterDetector).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance);

    [Test]
    public void Start_FindsPlayerAndMonsterHealthUI_AndHidesUI()
    {
        var playerGO = new GameObject("ScenePlayer");
        var player = playerGO.AddComponent<Player>();

        var uiGO = new GameObject("MonsterUI");
        var monsterUI = uiGO.AddComponent<MonsterHealthUI>();

        var tmpGO = new GameObject("TMP");
        tmpGO.transform.SetParent(uiGO.transform);
        var tmp = tmpGO.AddComponent<TextMeshProUGUI>();
        var barGO = new GameObject("Bar");
        barGO.transform.SetParent(uiGO.transform);
        var bar = barGO.AddComponent<Image>();
        bar.type = Image.Type.Filled;

        var detectorGO = new GameObject("Detector");
        var detector = detectorGO.AddComponent<PlayerMonsterDetector>();

        try
        {
            GetPrivateMethod("Start").Invoke(detector, null);

            var foundPlayer = (Player)GetPrivateField("player").GetValue(detector);
            var foundUI = (MonsterHealthUI)GetPrivateField("monsterHealthUI").GetValue(detector);

            Assert.IsNotNull(foundPlayer);
            Assert.IsNotNull(foundUI);
            Assert.IsFalse(monsterUI.gameObject.activeInHierarchy);
        }
        finally
        {
            Object.DestroyImmediate(playerGO);
            Object.DestroyImmediate(uiGO);
            Object.DestroyImmediate(tmpGO);
            Object.DestroyImmediate(barGO);
            Object.DestroyImmediate(detectorGO);
        }
    }

    [Test]
    public void Update_ShowsNearestMonster_WhenWithinRange()
    {
        var playerGO = new GameObject("Player");
        playerGO.transform.position = Vector3.zero;
        var player = playerGO.AddComponent<Player>();

        var m1GO = new GameObject("MonsterNear");
        m1GO.transform.position = new Vector3(2f, 0f, 0f);
        var m1 = m1GO.AddComponent<Dragon>();

        var m2GO = new GameObject("MonsterFar");
        m2GO.transform.position = new Vector3(4f, 0f, 0f);
        var m2 = m2GO.AddComponent<Dragon>();

        var uiGO = new GameObject("MonsterUI");
        var monsterUI = uiGO.AddComponent<MonsterHealthUI>();
        var tmpGO = new GameObject("TMP");
        tmpGO.transform.SetParent(uiGO.transform);
        var tmp = tmpGO.AddComponent<TextMeshProUGUI>();
        var barGO = new GameObject("Bar");
        barGO.transform.SetParent(uiGO.transform);
        var bar = barGO.AddComponent<Image>();
        bar.type = Image.Type.Filled;

        var detectorGO = new GameObject("Detector");
        var detector = detectorGO.AddComponent<PlayerMonsterDetector>();

        try
        {
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
        finally
        {
            Object.DestroyImmediate(playerGO);
            Object.DestroyImmediate(m1GO);
            Object.DestroyImmediate(m2GO);
            Object.DestroyImmediate(uiGO);
            Object.DestroyImmediate(tmpGO);
            Object.DestroyImmediate(barGO);
            Object.DestroyImmediate(detectorGO);
        }
    }

    [Test]
    public void Update_HidesAndClears_WhenNoMonsterNearby()
    {
        var playerGO = new GameObject("Player");
        playerGO.transform.position = Vector3.zero;
        var player = playerGO.AddComponent<Player>();

        var mGO = new GameObject("MonsterFarAway");
        mGO.transform.position = new Vector3(100f, 0f, 0f);
        var monster = mGO.AddComponent<Dragon>();

        var uiGO = new GameObject("MonsterUI");
        var monsterUI = uiGO.AddComponent<MonsterHealthUI>();
        var tmpGO = new GameObject("TMP");
        tmpGO.transform.SetParent(uiGO.transform);
        tmpGO.AddComponent<TextMeshProUGUI>();
        var barGO = new GameObject("Bar");
        barGO.transform.SetParent(uiGO.transform);
        var bar = barGO.AddComponent<Image>();
        bar.type = Image.Type.Filled;

        var detectorGO = new GameObject("Detector");
        var detector = detectorGO.AddComponent<PlayerMonsterDetector>();

        try
        {
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
        finally
        {
            Object.DestroyImmediate(playerGO);
            Object.DestroyImmediate(mGO);
            Object.DestroyImmediate(uiGO);
            Object.DestroyImmediate(tmpGO);
            Object.DestroyImmediate(barGO);
            Object.DestroyImmediate(detectorGO);
        }
    }
}
