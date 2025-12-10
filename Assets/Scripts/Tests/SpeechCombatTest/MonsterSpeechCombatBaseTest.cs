using NUnit.Framework;
using System.Reflection;
using UnityEngine;

/// <summary>
/// Tests af MonsterSpeechCombatBase2 ved hjælp af en lille test-subclass
/// og et letvægts TestMonster, så vi ikke er afhængige af de “rigtige” monstre.
/// </summary>
public class MonsterSpeechCombatBaseTest
{
    // Konkrete test-implementation der arver fra den nye base-klasse
    private class TestCombat : MonsterSpeechCombatBase
    {
        public bool ShowCalled;
        public string LastShownSentence;

        // Gør protected metoder public til tests
        public void StartNewRoundPublic() => StartNewRound();
        public void HandleTaskSuccessPublic() => HandleTaskSuccess();
        public void HandleTaskFailPublic() => HandleTaskFail();
        public void OnTriggerEnterPublic(Collider collider) => OnTriggerEnter(collider);
        public void PlayPlayerSwordAnimationPublic() => PlayPlayerSwordAnimation();

        protected override void ShowSentenceWithCorrectMode(string sentence)
        {
            ShowCalled = true;
            LastShownSentence = sentence;
        }
    }

    // Simpelt test-monster der bare bruger Monsters rigtige logik
    private class TestMonster : Monster
    {
        // tom – vi bruger Monsters egen adfærd
    }

    private Player CreatePlayer(int health = 10)
    {
        var playerGo = new GameObject("PlayerTest");
        var player = playerGo.AddComponent<Player>();

        typeof(Player).GetField("_maxHealth", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(player, health);
        typeof(Player).GetField("_currentHealth", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(player, health);

        return player;
    }

    [Test]
    public void Awake_SetsMonsterAndTriggerCollider()
    {
        // (Valgfrit) Lav en SpeechTaskUI i scenen – gør ingen skade
        var sceneUI = new GameObject("SpeechUI");
        sceneUI.AddComponent<SpeechTaskUI>();

        var go = new GameObject("TestCombatGO");
        go.AddComponent<SphereCollider>();
        go.AddComponent<TestMonster>();
        var combat = go.AddComponent<TestCombat>();

        var monsterField = typeof(MonsterSpeechCombatBase)
            .GetField("monster", BindingFlags.Instance | BindingFlags.NonPublic);
        var colliderField = typeof(MonsterSpeechCombatBase)
            .GetField("triggerCollider", BindingFlags.Instance | BindingFlags.NonPublic);

        // Tjek at Awake har sat monster og triggerCollider korrekt
        Assert.IsNotNull(monsterField.GetValue(combat), "Awake skal sætte monster-feltet.");

        var sphereCollider = (SphereCollider)colliderField.GetValue(combat);
        Assert.IsNotNull(sphereCollider, "Awake skal sætte triggerCollider.");
        Assert.IsTrue(sphereCollider.isTrigger, "Awake skal markere collideren som trigger.");

        // Vi tester ikke længere, at speechTaskUI automatisk bliver fundet,
        // fordi vi i praksis sætter den via Inspector i spillet.

        Object.DestroyImmediate(sceneUI);
        Object.DestroyImmediate(go);
    }

    [Test]
    public void StartNewRound_WithFightActive_AndHealthyMonster_ShowsSentenceAndSetsWaiting()
    {
        var go = new GameObject("TestCombatStartRound");
        go.AddComponent<SphereCollider>();
        var monster = go.AddComponent<TestMonster>();
        monster.CurrentHealth = 50;

        var combat = go.AddComponent<TestCombat>();

        var uiGo = new GameObject("SpeechUI2");
        var ui = uiGo.AddComponent<SpeechTaskUI>();

        typeof(MonsterSpeechCombatBase)
            .GetField("speechTaskUI", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(combat, ui);

        typeof(MonsterSpeechCombatBase)
            .GetField("isFightActive", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(combat, true);
        typeof(MonsterSpeechCombatBase)
            .GetField("isWaitingForResult", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(combat, false);
        typeof(MonsterSpeechCombatBase)
            .GetField("sentences", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(combat, new string[] { "hello" });

        combat.StartNewRoundPublic();

        Assert.IsTrue(combat.ShowCalled,
            "StartNewRound skal kalde ShowSentenceWithCorrectMode på den konkrete klasse.");

        var waiting = (bool)typeof(MonsterSpeechCombatBase)
            .GetField("isWaitingForResult", BindingFlags.Instance | BindingFlags.NonPublic)
            .GetValue(combat);

        Assert.IsTrue(waiting, "StartNewRound skal sætte isWaitingForResult = true.");

        Object.DestroyImmediate(uiGo);
        Object.DestroyImmediate(go);
    }

    [Test]
    public void GetNextSentence_HandlesEmptyAndSingleElementLists()
    {
        var go = new GameObject("GetNextSentenceGO");
        go.AddComponent<SphereCollider>();
        go.AddComponent<TestMonster>();
        var combat = go.AddComponent<TestCombat>();

        // Null/empty -> null
        typeof(MonsterSpeechCombatBase)
            .GetField("sentences", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(combat, null);

        var method = typeof(MonsterSpeechCombatBase)
            .GetMethod("GetNextSentence", BindingFlags.Instance | BindingFlags.NonPublic);

        var result = (string)method.Invoke(combat, null);
        Assert.IsNull(result, "GetNextSentence skal returnere null når der ikke er nogen sætninger.");

        // Single element -> altid det samme element (selv med Random.Range)
        typeof(MonsterSpeechCombatBase)
            .GetField("sentences", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(combat, new string[] { "only" });

        result = (string)method.Invoke(combat, null);
        Assert.AreEqual("only", result);

        Object.DestroyImmediate(go);
    }
}
