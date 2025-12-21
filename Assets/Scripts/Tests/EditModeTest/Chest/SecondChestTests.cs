using NUnit.Framework;
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.TestTools;

public class SecondChestTests
{
    private GameObject playerGameObject;
    /// <summary>
    /// Test-proxy for <see cref="SecondChest"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// I kiste-scripts er <c>CanOpen()</c> og <c>CreateLoot()</c> markeret som <c>protected</c>.
    /// Det betyder, at de kun kan kaldes inde fra selve klassen eller fra en subklasse, og ikke direkte
    /// fra en testklasse. Denne proxy arver derfor fra kisten og giver små public “wrapper”-metoder,
    /// så testen kan kalde logikken uden at ændre den originale scripts.
    /// </para>
    /// <para>
    /// Proxy’en ændrer ikke adfærden i spillet. Den er kun et hjælpe-objekt i tests, som gør det muligt
    /// at teste de beskyttede metoder på en enkel og tydelig måde.
    /// </para>
    private class SecondChestProxy : SecondChest
    {
        /// <summary>
        /// Eksponerer den beskyttede CanOpen-metode fra SecondChest.
        /// </summary>
        public bool CallCanOpen()
        {
            return base.CanOpen();
        }

        /// <summary>
        /// Eksponerer den beskyttede CreateLoot-metode fra SecondChest.
        /// </summary>
        public IChestLoot CallCreateLoot()
        {
            return base.CreateLoot();
        }
    }

    /// <summary>
    /// Opretter Player med tag og nødvendige komponenter, så Chest.Awake kan finde Player.
    /// </summary>
    [SetUp]
    public void SetUp()
    {
        playerGameObject = new GameObject("Player");
        playerGameObject.tag = "Player";
        playerGameObject.AddComponent<PlayerInventory>();
        playerGameObject.AddComponent<PlayerWeapon>();
    }

    /// <summary>
    /// Verificerer at CanOpen returnerer true, når requiredMonster ikke er sat.
    /// </summary>
    [Test]
    public void CanOpen_ReturnsTrue_WhenRequiredMonsterIsNull()
    {
        SecondChestProxy secondChest = CreateSecondChestWithColliderAndAnimatorChild();
        Assert.IsTrue(secondChest.CallCanOpen());
    }

    /// <summary>
    /// Verificerer at CanOpen returnerer true, når monsteret er besejret.
    /// </summary>
    [Test]
    public void CanOpen_ReturnsTrue_WhenMonsterDefeated()
    {
        SecondChestProxy secondChest = CreateSecondChestWithColliderAndAnimatorChild();

        Monster concreteMonster = CreateConcreteMonsterOrInconclusive();
        SetMonsterCurrentHealth(concreteMonster, 0);
        SetPrivateFieldValue(typeof(Chest), secondChest, "requiredMonster", concreteMonster);

        Assert.IsTrue(secondChest.CallCanOpen());

        UnityEngine.Object.DestroyImmediate(concreteMonster.gameObject);
    }

    /// <summary>
    /// Verificerer at CanOpen returnerer false, når monsteret stadig lever.
    /// </summary>
    [Test]
    public void CanOpen_ReturnsFalse_WhenMonsterAlive()
    {
        SecondChestProxy secondChest = CreateSecondChestWithColliderAndAnimatorChild();

        Monster concreteMonster = CreateConcreteMonsterOrInconclusive();
        SetMonsterCurrentHealth(concreteMonster, 10);
        SetPrivateFieldValue(typeof(Chest), secondChest, "requiredMonster", concreteMonster);

        Assert.IsFalse(secondChest.CallCanOpen());

        UnityEngine.Object.DestroyImmediate(concreteMonster.gameObject);
    }

    /// <summary>
    /// Verificerer at CreateLoot logger fejl og returnerer null, når weapon prefab mangler.
    /// </summary>
    [Test]
    public void CreateLoot_LogsError_AndReturnsNull_WhenWeaponMissing()
    {
        SecondChestProxy secondChest = CreateSecondChestWithColliderAndAnimatorChild();
        SetPrivateFieldValue(typeof(SecondChest), secondChest, "twoHandSwordWeaponPrefab", null);

        UnityEngine.TestTools.LogAssert.Expect(LogType.Error, "SecondChest: twoHandSwordWeaponPrefab er ikke sat i Inspector.");
        IChestLoot chestLoot = secondChest.CallCreateLoot();

        Assert.IsNull(chestLoot);
    }

    /// <summary>
    /// Verificerer at CreateLoot returnerer loot, når våben-prefab er sat.
    /// </summary>
    [Test]
    public void CreateLoot_ReturnsLoot_WhenWeaponAssigned()
    {
        SecondChestProxy secondChest = CreateSecondChestWithColliderAndAnimatorChild();

        Weapon twoHandSwordWeapon = new GameObject("TwoHandSword").AddComponent<TwoHandSword>();
        SetPrivateFieldValue(typeof(SecondChest), secondChest, "twoHandSwordWeaponPrefab", twoHandSwordWeapon);

        IChestLoot chestLoot = secondChest.CallCreateLoot();
        Assert.IsNotNull(chestLoot);

        UnityEngine.Object.DestroyImmediate(twoHandSwordWeapon.gameObject);
    }

    /// <summary>
    /// Verificerer at loot giver keyId=2 og potionAmount=1 til spillerens inventory.
    /// </summary>
    [Test]
    public void Loot_GivesExpectedKeyAndPotions()
    {
        SecondChestProxy secondChest = CreateSecondChestWithColliderAndAnimatorChild();

        Weapon twoHandSwordWeapon = new GameObject("TwoHandSword").AddComponent<TwoHandSword>();
        SetPrivateFieldValue(typeof(SecondChest), secondChest, "twoHandSwordWeaponPrefab", twoHandSwordWeapon);

        IChestLoot chestLoot = secondChest.CallCreateLoot();
        Assert.IsNotNull(chestLoot);

        PlayerInventory playerInventory = playerGameObject.GetComponent<PlayerInventory>();
        int potionCountBefore = playerInventory.GetPotionCount();

        chestLoot.GiveItemsToPlayer(playerInventory);

        Assert.IsTrue(playerInventory.HasKey(2));
        Assert.AreEqual(potionCountBefore + 1, playerInventory.GetPotionCount());

        UnityEngine.Object.DestroyImmediate(twoHandSwordWeapon.gameObject);
    }

    /// <summary>
    /// Opretter en SecondChest på et GameObject med BoxCollider og et child-objekt med Animator.
    /// </summary>
    private static SecondChestProxy CreateSecondChestWithColliderAndAnimatorChild()
    {
        GameObject secondChestGameObject = new GameObject("SecondChest");
        secondChestGameObject.AddComponent<BoxCollider>();

        GameObject animatorChildGameObject = new GameObject("AnimatorChild");
        animatorChildGameObject.transform.SetParent(secondChestGameObject.transform);
        animatorChildGameObject.AddComponent<Animator>();

        return secondChestGameObject.AddComponent<SecondChestProxy>();
    }

    /// <summary>
    /// Finder og opretter en konkret (ikke-abstract) subtype af Monster.
    /// </summary>
    private static Monster CreateConcreteMonsterOrInconclusive()
    {
        Type monsterBaseType = typeof(Monster);

        Type concreteMonsterType = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(assembly =>
            {
                try
                {
                    return assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException reflectionTypeLoadException)
                {
                    return reflectionTypeLoadException.Types.Where(type => type != null);
                }
            })
            .FirstOrDefault(type => monsterBaseType.IsAssignableFrom(type) && !type.IsAbstract);

        if (concreteMonsterType == null)
        {
            Assert.Inconclusive("Ingen konkret Monster-subtype blev fundet i projektet.");
        }

        GameObject monsterGameObject = new GameObject("TestMonster");
        Component monsterComponent = monsterGameObject.AddComponent(concreteMonsterType);
        return (Monster)monsterComponent;
    }

    /// <summary>
    /// Sætter monsterets CurrentHealth via reflection, uanset om det er property eller felt.
    /// </summary>
    private static void SetMonsterCurrentHealth(Monster monsterInstance, int currentHealthValue)
    {
        Type monsterRuntimeType = monsterInstance.GetType();

        PropertyInfo currentHealthPropertyInfo = monsterRuntimeType.GetProperty(
            "CurrentHealth",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
        );

        if (currentHealthPropertyInfo != null && currentHealthPropertyInfo.CanWrite)
        {
            currentHealthPropertyInfo.SetValue(monsterInstance, currentHealthValue);
            return;
        }

        FieldInfo currentHealthFieldInfo =
            monsterRuntimeType.GetField("CurrentHealth", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance) ??
            monsterRuntimeType.GetField("currentHealth", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance) ??
            monsterRuntimeType.GetField("_currentHealth", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        Assert.IsNotNull(currentHealthFieldInfo, "Kunne ikke sætte CurrentHealth på monsteret via reflection.");
        currentHealthFieldInfo.SetValue(monsterInstance, currentHealthValue);
    }

    /// <summary>
    /// Sætter en privat field-værdi via reflection på en given target-instans.
    /// </summary>
    private static void SetPrivateFieldValue(Type declaringType, object targetInstance, string privateFieldName, object fieldValue)
    {
        FieldInfo privateFieldInfo = declaringType.GetField(privateFieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(privateFieldInfo, $"{declaringType.Name}.{privateFieldName} blev ikke fundet via reflection.");
        privateFieldInfo.SetValue(targetInstance, fieldValue);
    }
}