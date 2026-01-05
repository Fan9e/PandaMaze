using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.Events;

public class MonsterDragonTest
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
    /// Test-udgave af Dragon der gør CreatePresenter tilgængelig for tests.
    /// </summary>
    private sealed class DragonForPresenterTest : Dragon
    {
        /// <summary>
        /// Returnerer den presenter som Dragon laver via template method.
        /// </summary>
        public ISpeechTaskPresenter CreatePresenterForTest()
        {
            return CreatePresenter();
        }
    }

    /// <summary>
    /// Tester at Dragon’s template method CreatePresenter returnerer NormalPresenter.
    /// </summary>
    [Test]
    public void Presenter_Dragon_CreatePresenter_ReturnsNormalPresenter()
    {
        GameObject dragonGameObject = new GameObject("Dragon_Presenter_Test");
        DragonForPresenterTest dragonComponent = dragonGameObject.AddComponent<DragonForPresenterTest>();

        ISpeechTaskPresenter presenter = dragonComponent.CreatePresenterForTest();

        Assert.NotNull(presenter);
        Assert.AreEqual("NormalPresenter", presenter.GetType().Name);

        DestroyImmediately(dragonGameObject);
    }

    /// <summary>
    /// Tester at Dragon tager skade og giver skade til spilleren via Fight().
    /// </summary>
    [Test]
    public void Fight_DragonTakesDamage_AndDealsDamageToPlayer()
    {
        var monsterGameObject = new GameObject();
        var monster = monsterGameObject.AddComponent<Dragon>();

        var playerGameObject = new GameObject();
        var player = playerGameObject.AddComponent<Player>();

        monster.MaxHealth = 20;
        monster.CurrentHealth = 20;
        monster.AttackPower = 5;
        player.MaxHealth = 30;
        player.CurrentHealth = 30;
        monster.Player = player;
        monster.Fight(7, false);

        Assert.AreEqual(13, monster.CurrentHealth);
        Assert.AreEqual(25, player.CurrentHealth);

        DestroyImmediately(monsterGameObject, playerGameObject);
    }

    /// <summary>
    /// Tester at Dragon dør når health når 0 (eller under) via Fight().
    /// </summary>
    [Test]
    public void Fight_DragonDies_WhenHealthReachesZero()
    {
        var monsterGameObject = new GameObject();
        var monster = monsterGameObject.AddComponent<Dragon>();
        var playerGameObject = new GameObject();
        var player = playerGameObject.AddComponent<Player>();

        monster.MaxHealth = 15;
        monster.CurrentHealth = 15;
        monster.AttackPower = 5;
        player.MaxHealth = 30;
        player.CurrentHealth = 30;
        monster.Player = player;
        monster.Fight(20, false);

        Assert.AreEqual(0, monster.CurrentHealth);
        Assert.AreEqual(30, player.CurrentHealth);

        DestroyImmediately(monsterGameObject, playerGameObject);
    }

    /// <summary>
    /// Tester at negativ damage ikke ændrer monsterets health via Fight().
    /// </summary>
    [Test]
    public void Fight_NegativeDamage_DoesNotChangeHealth()
    {
        var monsterGameObject = new GameObject();
        var monster = monsterGameObject.AddComponent<Dragon>();
        var playerGameObject = new GameObject();
        var player = playerGameObject.AddComponent<Player>();

        monster.MaxHealth = 20;
        monster.CurrentHealth = 20;
        monster.AttackPower = 5;
        monster.Player = player;
        monster.Fight(-5, true);

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
        var monster = monsterGameObject.AddComponent<Dragon>();
        monster.MaxHealth = 20;
        monster.CurrentHealth = 20;
        monster.AttackPower = 5;
        monster.Player = null;
        monster.Fight(5, false);

        Assert.AreEqual(20, monster.CurrentHealth);

        DestroyImmediately(monsterGameObject);
    }
    /// <summary>
    /// Tester at Fight() ikke giver Player skade hvis den sætning er sagt rigtig.
    /// </summary>
    [Test]
    public void Fight_DoesNothing_IfSentenceWasCorrect()
    {
        var monsterGameObject = new GameObject();
        var monster = monsterGameObject.AddComponent<Dragon>();
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
    /// Tester at Fight() ikke ændrer noget hvis Dragon allerede er død.
    /// </summary>
    [Test]
    public void Fight_DoesNothing_IfDragonAlreadyDead()
    {
        var monsterGameObject = new GameObject();
        var monster = monsterGameObject.AddComponent<Dragon>();
        var playerGameObject = new GameObject();
        var player = playerGameObject.AddComponent<Player>();

        monster.MaxHealth = 10;
        monster.CurrentHealth = 0;
        monster.AttackPower = 5;
        player.MaxHealth = 30;
        player.CurrentHealth = 30;
        monster.Player = player;
        monster.Fight(5, false);

        Assert.AreEqual(0, monster.CurrentHealth);
        Assert.AreEqual(30, player.CurrentHealth);

        DestroyImmediately(monsterGameObject, playerGameObject);
    }

    /// <summary>
    /// Tester at spilleren ikke tager skade hvis Dragon dør af damage i samme Fight() kald.
    /// </summary>
    [Test]
    public void Fight_DoesNothing_IfDragonDiesFromDamage()
    {
        var monsterGameObject = new GameObject();
        var monster = monsterGameObject.AddComponent<Dragon>();
        var playerGameObject = new GameObject();
        var player = playerGameObject.AddComponent<Player>();

        monster.MaxHealth = 10;
        monster.CurrentHealth = 10;
        monster.AttackPower = 5;
        player.MaxHealth = 30;
        player.CurrentHealth = 30;
        monster.Player = player;
        monster.Fight(15, false);

        Assert.AreEqual(0, monster.CurrentHealth);
        Assert.AreEqual(30, player.CurrentHealth);

        DestroyImmediately(monsterGameObject, playerGameObject);
    }

    /// <summary>
    /// Tester at Dragon starter speech-combat ved trigger-enter: UI aktiveres, state flags sættes,
    /// og sentence sættes.
    /// Forventer at presenter er NoOperationSpeechTaskPresenter, fordi vi overstyrer presenter i tests.
    /// </summary>
    [Test]
    public void Speech_Dragon_TriggerEnter_StartsSpeechCombat_ActivatesUserInterface_SetsState_AndPresenterType()
    {
        SpeechTaskUI speechTaskUserInterface = CreateActiveSpeechTaskUserInterface();
        Player playerComponent = CreatePlayerWithCollider();
        Dragon dragonComponent = CreateDragonWithInjectedSpeechDependencies(speechTaskUserInterface, new[] { "Dragon one", "Dragon two" });

        speechTaskUserInterface.gameObject.SetActive(false);

        InvokeOnTriggerEnter(dragonComponent, playerComponent.GetComponent<Collider>());

        Assert.IsTrue(speechTaskUserInterface.gameObject.activeSelf);
        Assert.IsTrue(GetPrivateInstanceField<bool>(dragonComponent, "isFightActive"));
        Assert.IsTrue(GetPrivateInstanceField<bool>(dragonComponent, "isWaitingForResult"));
        Assert.IsFalse(string.IsNullOrEmpty(GetPrivateInstanceField<string>(dragonComponent, "currentSentence")));

        object presenterInstance = GetPrivateInstanceField<object>(dragonComponent, "presenter");
        Assert.NotNull(presenterInstance);
        Assert.AreEqual("NoOperationSpeechTaskPresenter", presenterInstance.GetType().Name);

        DestroyImmediately(dragonComponent.gameObject, playerComponent.gameObject, speechTaskUserInterface.gameObject);
    }

    /// <summary>
    /// Tester at Dragon ved fail genbruger samme sætning og går tilbage i “venter på resultat”.
    /// </summary>
    [Test]
    public void Speech_Dragon_Failure_ReusesSameSentence_AndWaitsForResultAgain()
    {
        SpeechTaskUI speechTaskUserInterface = CreateActiveSpeechTaskUserInterface();
        Player playerComponent = CreatePlayerWithCollider();
        Dragon dragonComponent = CreateDragonWithInjectedSpeechDependencies(speechTaskUserInterface, new[] { "Dragon one", "Dragon two" });

        speechTaskUserInterface.gameObject.SetActive(false);

        InvokeOnTriggerEnter(dragonComponent, playerComponent.GetComponent<Collider>());

        string sentenceBeforeFailure = GetPrivateInstanceField<string>(dragonComponent, "currentSentence");

        UnityEvent onTaskFailedEvent = GetOrCreateUnityEvent(speechTaskUserInterface, "OnTaskFailed");
        onTaskFailedEvent.Invoke();

        string sentenceAfterFailure = GetPrivateInstanceField<string>(dragonComponent, "currentSentence");

        Assert.AreEqual(sentenceBeforeFailure, sentenceAfterFailure);
        Assert.IsTrue(GetPrivateInstanceField<bool>(dragonComponent, "isWaitingForResult"));
        Assert.IsTrue(speechTaskUserInterface.gameObject.activeSelf);

        DestroyImmediately(dragonComponent.gameObject, playerComponent.gameObject, speechTaskUserInterface.gameObject);
    }

    /// <summary>
    /// Tester at Dragon ved success starter en ny runde og igen venter på resultat.
    /// (Der testes ikke “ny sætning” pga. Random kan vælge samme.)
    /// </summary>
    [Test]
    public void Speech_Dragon_Success_StartsNewRound_AndWaitsForResultAgain()
    {
        SpeechTaskUI speechTaskUserInterface = CreateActiveSpeechTaskUserInterface();
        Player playerComponent = CreatePlayerWithCollider();
        Dragon dragonComponent = CreateDragonWithInjectedSpeechDependencies(speechTaskUserInterface, new[] { "Dragon one", "Dragon two" });

        speechTaskUserInterface.gameObject.SetActive(false);

        InvokeOnTriggerEnter(dragonComponent, playerComponent.GetComponent<Collider>());

        UnityEvent onTaskCompletedEvent = GetOrCreateUnityEvent(speechTaskUserInterface, "OnTaskCompleted");
        onTaskCompletedEvent.Invoke();

        Assert.IsTrue(speechTaskUserInterface.gameObject.activeSelf);
        Assert.IsTrue(GetPrivateInstanceField<bool>(dragonComponent, "isWaitingForResult"));
        Assert.IsFalse(string.IsNullOrEmpty(GetPrivateInstanceField<string>(dragonComponent, "currentSentence")));

        DestroyImmediately(dragonComponent.gameObject, playerComponent.gameObject, speechTaskUserInterface.gameObject);
    }

    /// <summary>
    /// Tester at fallback-sætningen bruges, hvis sentences er tom.
    /// </summary>
    [Test]
    public void Speech_Dragon_UsesFallbackSentence_WhenNoSentencesAreConfigured()
    {
        SpeechTaskUI speechTaskUserInterface = CreateActiveSpeechTaskUserInterface();
        Player playerComponent = CreatePlayerWithCollider();
        Dragon dragonComponent = CreateDragonWithInjectedSpeechDependencies(speechTaskUserInterface, new string[0]);

        speechTaskUserInterface.gameObject.SetActive(false);

        InvokeOnTriggerEnter(dragonComponent, playerComponent.GetComponent<Collider>());

        string currentSentence = GetPrivateInstanceField<string>(dragonComponent, "currentSentence");
        Assert.AreEqual("Denne sætning skal hjælp med at udtale orden", currentSentence);

        DestroyImmediately(dragonComponent.gameObject, playerComponent.gameObject, speechTaskUserInterface.gameObject);
    }

    /// <summary>
    /// Opretter et aktivt SpeechTaskUI GameObject og sikrer at UnityEvents ikke er null,
    /// så Monster.OnEnable ikke crasher når den forsøger at AddListener.
    /// </summary>
    private static SpeechTaskUI CreateActiveSpeechTaskUserInterface()
    {
        GameObject speechTaskUserInterfaceGameObject = new GameObject("SpeechTaskUI_Test_Dragon");
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
        GameObject playerGameObject = new GameObject("Player_Speech_Test_Dragon");
        Player playerComponent = playerGameObject.AddComponent<Player>();
        playerComponent.CurrentHealth = 9999;
        playerGameObject.AddComponent<CapsuleCollider>();
        return playerComponent;
    }

    /// <summary>
    /// Opretter Dragon og injicer speechTaskUI + sentences før Awake/OnEnable.
    /// Overstyrer presenter EFTER Awake har kørt, så den ikke bliver overskrevet af CreatePresenter().
    /// </summary>
    private static Dragon CreateDragonWithInjectedSpeechDependencies(SpeechTaskUI speechTaskUserInterface, string[] sentences)
    {
        GameObject dragonGameObject = new GameObject("Dragon_Speech_Test");
        dragonGameObject.SetActive(false);

        Dragon dragonComponent = dragonGameObject.AddComponent<Dragon>();

        SphereCollider sphereCollider = dragonGameObject.GetComponent<SphereCollider>();
        if (sphereCollider != null)
        {
            sphereCollider.isTrigger = true;
            sphereCollider.radius = 2f;
        }

        dragonComponent.MaxHealth = 9999;
        dragonComponent.CurrentHealth = 9999;
        dragonComponent.AttackPower = 0;

        SetPrivateInstanceField(dragonComponent, "speechTaskUI", speechTaskUserInterface);
        SetPrivateInstanceField(dragonComponent, "sentences", sentences);

        // Aktivér: Awake() kører her og vil lave presenter = CreatePresenter()
        dragonGameObject.SetActive(true);

        // VIGTIGT: Overstyr presenter EFTER Awake, så StartNewRound bruger vores NoOp presenter
        SetPrivateInstanceField(dragonComponent, "presenter", new NoOperationSpeechTaskPresenter());

        return dragonComponent;
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
    private static void InvokeOnTriggerEnter(Dragon dragonComponent, Collider otherCollider)
    {
        MethodInfo onTriggerEnterMethodInfo = FindMethodInTypeHierarchy(dragonComponent.GetType(), "OnTriggerEnter");
        Assert.NotNull(onTriggerEnterMethodInfo, "OnTriggerEnter(Collider) blev ikke fundet i type-hierarkiet.");
        onTriggerEnterMethodInfo.Invoke(dragonComponent, new object[] { otherCollider });
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
