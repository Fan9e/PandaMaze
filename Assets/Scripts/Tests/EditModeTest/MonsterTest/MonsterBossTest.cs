using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.Events;

public class MonsterBossTest
{
    /// <summary>
    /// Minimal presenter der ikke gør noget.
    /// Bruges så tests ikke crasher, hvis rigtige presenters forventer UI-refs.
    /// </summary>
    private sealed class NoOperationSpeechTaskPresenter : ISpeechTaskPresenter
    {
        /// <summary>
        /// Gør intet i test.
        /// </summary>
        public void Show(SpeechTaskUI speechTaskUserInterface, string sentence)
        {
        }
    }

    /// <summary>
    /// Test-udgave af Boss der gør CreatePresenter tilgængelig for tests.
    /// </summary>
    private sealed class BossForPresenterTest : Boss
    {
        /// <summary>
        /// Returnerer den presenter som Boss laver via template method.
        /// </summary>
        public ISpeechTaskPresenter CreatePresenterForTest()
        {
            return CreatePresenter();
        }
    }

    /// <summary>
    /// Tester at Boss’s template method CreatePresenter returnerer FullScramblePresenter.
    /// </summary>
    [Test]
    public void Presenter_Boss_CreatePresenter_ReturnsFullScramblePresenter()
    {
        GameObject bossGameObject = new GameObject("Boss_Presenter_Test");
        BossForPresenterTest bossComponent = bossGameObject.AddComponent<BossForPresenterTest>();

        ISpeechTaskPresenter presenter = bossComponent.CreatePresenterForTest();

        Assert.NotNull(presenter);
        Assert.AreEqual("FullScramblePresenter", presenter.GetType().Name);

        DestroyImmediately(bossGameObject);
    }


    /// <summary>
    /// Tester at Boss tager skade og giver skade til spilleren via Fight().
    /// </summary>
    [Test]
    public void Fight_BossTakesDamage_AndDealsDamageToPlayer()
    {
        var monsterGameObject = new GameObject();
        var monster = monsterGameObject.AddComponent<Boss>();

        var playerGameObject = new GameObject();
        var player = playerGameObject.AddComponent<Player>();

        monster.MaxHealth = 60;
        monster.CurrentHealth = 60;
        monster.AttackPower = 20;
        player.MaxHealth = 100;
        player.CurrentHealth = 100;
        monster.Player = player;
        monster.Fight(7, false);

        Assert.AreEqual(53, monster.CurrentHealth);
        Assert.AreEqual(80, player.CurrentHealth);

        DestroyImmediately(monsterGameObject, playerGameObject);
    }

    /// <summary>
    /// Tester at Boss dør når health når 0 via Fight().
    /// </summary>
    [Test]
    public void Fight_BossDies_WhenHealthReachesZero()
    {
        var monsterGameObject = new GameObject();
        var monster = monsterGameObject.AddComponent<Boss>();
        var playerGameObject = new GameObject();
        var player = playerGameObject.AddComponent<Player>();

        monster.MaxHealth = 50;
        monster.CurrentHealth = 20;
        monster.AttackPower = 20;
        player.MaxHealth = 30;
        player.CurrentHealth = 60;
        monster.Player = player;
        monster.Fight(20, false);

        Assert.AreEqual(0, monster.CurrentHealth);

        DestroyImmediately(monsterGameObject, playerGameObject);
    }

    /// <summary>
    /// Tester at negativ damage ikke ændrer monsterets health via Fight().
    /// </summary>
    [Test]
    public void Fight_NegativeDamage_DoesNotChangeHealth()
    {
        var monsterGameObject = new GameObject();
        var monster = monsterGameObject.AddComponent<Boss>();
        var playerGameObject = new GameObject();
        var player = playerGameObject.AddComponent<Player>();

        monster.MaxHealth = 20;
        monster.CurrentHealth = 20;
        monster.AttackPower = 20;
        monster.Player = player;
        monster.Fight(-5, false);

        Assert.AreEqual(20, monster.CurrentHealth);

        DestroyImmediately(monsterGameObject, playerGameObject);
    }

    /// <summary>
    /// Tester at Fight() ikke gør noget hvis ingen Player er sat.
    /// </summary>
    [Test]
    public void Fight_DoesNothing_IfNoPlayerAssigned()
    {
        var monsterGameObject = new GameObject();
        var monster = monsterGameObject.AddComponent<Boss>();
        monster.MaxHealth = 20;
        monster.CurrentHealth = 20;
        monster.AttackPower = 20;
        monster.Player = null;
        monster.Fight(5, false);

        Assert.AreEqual(20, monster.CurrentHealth);

        DestroyImmediately(monsterGameObject);
    }

    /// <summary>
    /// Tester at Fight() ikke ændrer noget hvis Boss allerede er død.
    /// </summary>
    [Test]
    public void Fight_DoesNothing_IfBossAlreadyDead()
    {
        var monsterGameObject = new GameObject();
        var monster = monsterGameObject.AddComponent<Boss>();
        var playerGameObject = new GameObject();
        var player = playerGameObject.AddComponent<Player>();

        monster.MaxHealth = 10;
        monster.CurrentHealth = 0;
        monster.AttackPower = 20;
        player.MaxHealth = 30;
        player.CurrentHealth = 30;
        monster.Player = player;
        monster.Fight(5, false);

        Assert.AreEqual(0, monster.CurrentHealth);
        Assert.AreEqual(30, player.CurrentHealth);

        DestroyImmediately(monsterGameObject, playerGameObject);
    }

    /// <summary>
    /// Tester at spilleren ikke tager skade hvis Boss dør af damage i samme Fight() kald.
    /// </summary>
    [Test]
    public void Fight_DoesNothing_IfBossDiesFromDamage()
    {
        var monsterGameObject = new GameObject();
        var monster = monsterGameObject.AddComponent<Boss>();
        var playerGameObject = new GameObject();
        var player = playerGameObject.AddComponent<Player>();

        monster.MaxHealth = 10;
        monster.CurrentHealth = 10;
        monster.AttackPower = 20;
        player.MaxHealth = 30;
        player.CurrentHealth = 30;
        monster.Player = player;
        monster.Fight(15, false);

        Assert.AreEqual(0, monster.CurrentHealth);
        Assert.AreEqual(30, player.CurrentHealth);

        DestroyImmediately(monsterGameObject, playerGameObject);
    }
    /// <summary>
    /// Tester at Fight() ikke giver Player skade hvis den true.
    /// </summary>
    [Test]
    public void Fight_DoesNothing_IfSentenceWasCorrect()
    {
        var monsterGameObject = new GameObject();
        var monster = monsterGameObject.AddComponent<Cat>();
        var playerGameObject = new GameObject();
        var player = playerGameObject.AddComponent<Player>();
        player.MaxHealth = 30;
        player.CurrentHealth = 30;
        monster.MaxHealth = 20;
        monster.CurrentHealth = 20;
        monster.AttackPower = 5;
        monster.Player = player;
        monster.Fight(0, true);

        Assert.AreEqual(20, monster.CurrentHealth);
        Assert.AreEqual(30, player.CurrentHealth);

        DestroyImmediately(monsterGameObject);
    }

    /// <summary>
    /// Tester at Boss starter speech-combat ved trigger-enter: UI aktiveres, state flags sættes,
    /// og sentence sættes.
    /// Vi forventer at presenter er NoOperationSpeechTaskPresenter, fordi vi overstyrer presenter i tests.
    /// </summary>
    [Test]
    public void Speech_Boss_TriggerEnter_StartsSpeechCombat_ActivatesUserInterface_SetsState_AndPresenterType()
    {
        SpeechTaskUI speechTaskUserInterface = CreateActiveSpeechTaskUserInterface();
        Player playerComponent = CreatePlayerWithCollider();
        Boss bossComponent = CreateBossWithInjectedSpeechDependencies(speechTaskUserInterface, new[] { "Boss one", "Boss two" });

        speechTaskUserInterface.gameObject.SetActive(false);

        InvokeOnTriggerEnter(bossComponent, playerComponent.GetComponent<Collider>());

        Assert.IsTrue(speechTaskUserInterface.gameObject.activeSelf);
        Assert.IsTrue(GetPrivateInstanceField<bool>(bossComponent, "isFightActive"));
        Assert.IsTrue(GetPrivateInstanceField<bool>(bossComponent, "isWaitingForResult"));
        Assert.IsFalse(string.IsNullOrEmpty(GetPrivateInstanceField<string>(bossComponent, "currentSentence")));

        object presenterInstance = GetPrivateInstanceField<object>(bossComponent, "presenter");
        Assert.NotNull(presenterInstance);
        Assert.AreEqual("NoOperationSpeechTaskPresenter", presenterInstance.GetType().Name);

        DestroyImmediately(bossComponent.gameObject, playerComponent.gameObject, speechTaskUserInterface.gameObject);
    }

    /// <summary>
    /// Tester at Boss ved fail genbruger samme sætning og går tilbage i “venter på resultat”.
    /// </summary>
    [Test]
    public void Speech_Boss_Failure_ReusesSameSentence_AndWaitsForResultAgain()
    {
        SpeechTaskUI speechTaskUserInterface = CreateActiveSpeechTaskUserInterface();
        Player playerComponent = CreatePlayerWithCollider();
        Boss bossComponent = CreateBossWithInjectedSpeechDependencies(speechTaskUserInterface, new[] { "Boss one", "Boss two" });

        speechTaskUserInterface.gameObject.SetActive(false);

        InvokeOnTriggerEnter(bossComponent, playerComponent.GetComponent<Collider>());

        string sentenceBeforeFailure = GetPrivateInstanceField<string>(bossComponent, "currentSentence");

        UnityEvent onTaskFailedEvent = GetOrCreateUnityEvent(speechTaskUserInterface, "OnTaskFailed");
        onTaskFailedEvent.Invoke();

        string sentenceAfterFailure = GetPrivateInstanceField<string>(bossComponent, "currentSentence");

        Assert.AreEqual(sentenceBeforeFailure, sentenceAfterFailure);
        Assert.IsTrue(GetPrivateInstanceField<bool>(bossComponent, "isWaitingForResult"));
        Assert.IsTrue(speechTaskUserInterface.gameObject.activeSelf);

        DestroyImmediately(bossComponent.gameObject, playerComponent.gameObject, speechTaskUserInterface.gameObject);
    }

    /// <summary>
    /// Tester at Boss ved success starter en ny runde og igen venter på resultat.
    /// (Vi tester ikke “ny sætning” pga. Random kan vælge samme.)
    /// </summary>
    [Test]
    public void Speech_Boss_Success_StartsNewRound_AndWaitsForResultAgain()
    {
        SpeechTaskUI speechTaskUserInterface = CreateActiveSpeechTaskUserInterface();
        Player playerComponent = CreatePlayerWithCollider();
        Boss bossComponent = CreateBossWithInjectedSpeechDependencies(speechTaskUserInterface, new[] { "Boss one", "Boss two" });

        speechTaskUserInterface.gameObject.SetActive(false);

        InvokeOnTriggerEnter(bossComponent, playerComponent.GetComponent<Collider>());

        UnityEvent onTaskCompletedEvent = GetOrCreateUnityEvent(speechTaskUserInterface, "OnTaskCompleted");
        onTaskCompletedEvent.Invoke();

        Assert.IsTrue(speechTaskUserInterface.gameObject.activeSelf);
        Assert.IsTrue(GetPrivateInstanceField<bool>(bossComponent, "isWaitingForResult"));
        Assert.IsFalse(string.IsNullOrEmpty(GetPrivateInstanceField<string>(bossComponent, "currentSentence")));

        DestroyImmediately(bossComponent.gameObject, playerComponent.gameObject, speechTaskUserInterface.gameObject);
    }

    /// <summary>
    /// Tester at fallback-sætningen bruges, hvis sentences er tom.
    /// </summary>
    [Test]
    public void Speech_Boss_UsesFallbackSentence_WhenNoSentencesAreConfigured()
    {
        SpeechTaskUI speechTaskUserInterface = CreateActiveSpeechTaskUserInterface();
        Player playerComponent = CreatePlayerWithCollider();
        Boss bossComponent = CreateBossWithInjectedSpeechDependencies(speechTaskUserInterface, new string[0]);

        speechTaskUserInterface.gameObject.SetActive(false);

        InvokeOnTriggerEnter(bossComponent, playerComponent.GetComponent<Collider>());

        string currentSentence = GetPrivateInstanceField<string>(bossComponent, "currentSentence");
        Assert.AreEqual("Denne sætning skal hjælp med at udtale orden", currentSentence);

        DestroyImmediately(bossComponent.gameObject, playerComponent.gameObject, speechTaskUserInterface.gameObject);
    }

    /// <summary>
    /// Opretter et aktivt SpeechTaskUI GameObject og sikrer at UnityEvents ikke er null,
    /// så Monster.OnEnable ikke crasher når den forsøger at AddListener.
    /// </summary>
    private static SpeechTaskUI CreateActiveSpeechTaskUserInterface()
    {
        GameObject speechTaskUserInterfaceGameObject = new GameObject("SpeechTaskUI_Test_Boss");
        speechTaskUserInterfaceGameObject.SetActive(true);

        SpeechTaskUI speechTaskUserInterface = speechTaskUserInterfaceGameObject.AddComponent<SpeechTaskUI>();

        GetOrCreateUnityEvent(speechTaskUserInterface, "OnTaskCompleted");
        GetOrCreateUnityEvent(speechTaskUserInterface, "OnTaskFailed");

        return speechTaskUserInterface;
    }

    /// <summary>
    /// Opretter en Player med collider, så den kan sendes ind i OnTriggerEnter.
    /// </summary>
    private static Player CreatePlayerWithCollider()
    {
        GameObject playerGameObject = new GameObject("Player_Speech_Test_Boss");
        Player playerComponent = playerGameObject.AddComponent<Player>();
        playerComponent.CurrentHealth = 9999;
        playerGameObject.AddComponent<CapsuleCollider>();
        return playerComponent;
    }

    /// <summary>
    /// Opretter Boss og injicer speechTaskUI + sentences før Awake/OnEnable.
    /// Overstyrer presenter EFTER Awake har kørt, så den ikke bliver overskrevet af CreatePresenter().
    /// </summary>
    private static Boss CreateBossWithInjectedSpeechDependencies(SpeechTaskUI speechTaskUserInterface, string[] sentences)
    {
        GameObject bossGameObject = new GameObject("Boss_Speech_Test");
        bossGameObject.SetActive(false);

        Boss bossComponent = bossGameObject.AddComponent<Boss>();

        SphereCollider sphereCollider = bossGameObject.GetComponent<SphereCollider>();
        if (sphereCollider != null)
        {
            sphereCollider.isTrigger = true;
            sphereCollider.radius = 2f;
        }

        bossComponent.MaxHealth = 9999;
        bossComponent.CurrentHealth = 9999;
        bossComponent.AttackPower = 0;

        SetPrivateInstanceField(bossComponent, "speechTaskUI", speechTaskUserInterface);
        SetPrivateInstanceField(bossComponent, "sentences", sentences);

        bossGameObject.SetActive(true);

        SetPrivateInstanceField(bossComponent, "presenter", new NoOperationSpeechTaskPresenter());

        return bossComponent;
    }

    /// <summary>
    /// Finder et felt i typen eller en af dens base-typer (inkl. private felter i base-klasser).
    /// </summary>
    private static FieldInfo FindFieldInTypeHierarchy(System.Type startType, string fieldName)
    {
        System.Type currentType = startType;

        while (currentType != null)
        {
            FieldInfo fieldInfo = currentType.GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
            );

            if (fieldInfo != null)
            {
                return fieldInfo;
            }

            currentType = currentType.BaseType;
        }

        return null;
    }

    /// <summary>
    /// Finder en metode i typen eller en af dens base-typer (inkl. private metoder i base-klasser).
    /// </summary>
    private static MethodInfo FindMethodInTypeHierarchy(System.Type startType, string methodName)
    {
        System.Type currentType = startType;

        while (currentType != null)
        {
            MethodInfo methodInfo = currentType.GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
            );

            if (methodInfo != null)
            {
                return methodInfo;
            }

            currentType = currentType.BaseType;
        }

        return null;
    }

    /// <summary>
    /// Kalder OnTriggerEnter(Collider) via reflection (virker selv om metoden ligger i base-klassen).
    /// </summary>
    private static void InvokeOnTriggerEnter(Boss bossComponent, Collider otherCollider)
    {
        MethodInfo onTriggerEnterMethodInfo = FindMethodInTypeHierarchy(bossComponent.GetType(), "OnTriggerEnter");
        Assert.NotNull(onTriggerEnterMethodInfo, "OnTriggerEnter(Collider) blev ikke fundet i type-hierarkiet.");
        onTriggerEnterMethodInfo.Invoke(bossComponent, new object[] { otherCollider });
    }

    /// <summary>
    /// Læser private felter via reflection (virker også for private felter i base-klasser).
    /// </summary>
    private static TField GetPrivateInstanceField<TField>(object instance, string fieldName)
    {
        FieldInfo privateFieldInfo = FindFieldInTypeHierarchy(instance.GetType(), fieldName);
        Assert.NotNull(privateFieldInfo, $"Feltet '{fieldName}' blev ikke fundet i type-hierarkiet for {instance.GetType().Name}.");
        return (TField)privateFieldInfo.GetValue(instance);
    }

    /// <summary>
    /// Sætter private felter via reflection (virker også for private felter i base-klasser).
    /// </summary>
    private static void SetPrivateInstanceField(object instance, string fieldName, object value)
    {
        FieldInfo privateFieldInfo = FindFieldInTypeHierarchy(instance.GetType(), fieldName);
        Assert.NotNull(privateFieldInfo, $"Feltet '{fieldName}' blev ikke fundet i type-hierarkiet for {instance.GetType().Name}.");
        privateFieldInfo.SetValue(instance, value);
    }

    /// <summary>
    /// Finder eller opretter en UnityEvent (felt eller property) på SpeechTaskUI.
    /// </summary>
    private static UnityEvent GetOrCreateUnityEvent(object instance, string memberName)
    {
        System.Type instanceType = instance.GetType();

        FieldInfo fieldInfo = instanceType.GetField(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (fieldInfo != null && typeof(UnityEvent).IsAssignableFrom(fieldInfo.FieldType))
        {
            UnityEvent unityEventValue = (UnityEvent)fieldInfo.GetValue(instance);
            if (unityEventValue == null)
            {
                unityEventValue = new UnityEvent();
                fieldInfo.SetValue(instance, unityEventValue);
            }
            return unityEventValue;
        }

        PropertyInfo propertyInfo = instanceType.GetProperty(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (propertyInfo != null && typeof(UnityEvent).IsAssignableFrom(propertyInfo.PropertyType))
        {
            UnityEvent unityEventValue = (UnityEvent)propertyInfo.GetValue(instance);
            if (unityEventValue == null)
            {
                unityEventValue = new UnityEvent();
                propertyInfo.SetValue(instance, unityEventValue);
            }
            return unityEventValue;
        }

        Assert.Fail($"Kunne ikke finde UnityEvent medlem '{memberName}' på {instanceType.Name}.");
        return null;
    }

    /// <summary>
    /// DestroyImmediate oprydning i EditMode.
    /// </summary>
    private static void DestroyImmediately(params Object[] unityObjects)
    {
        foreach (Object unityObject in unityObjects)
        {
            if (unityObject == null) continue;

            if (unityObject is GameObject unityGameObject)
            {
                Object.DestroyImmediate(unityGameObject);
            }
            else
            {
                Object.DestroyImmediate(unityObject);
            }
        }
    }
}
