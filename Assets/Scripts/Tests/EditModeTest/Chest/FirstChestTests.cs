using NUnit.Framework;
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.TestTools;

public class FirstChestTests
{
    private GameObject playerGo;

    /// <summary>
    /// Test-proxy for <see cref="FirstChest"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// I kiste-scripts er <c>CanOpen()</c> og <c>CreateLoot()</c> markeret som <c>protected</c>.
    /// Det betyder, at de kun kan kaldes inde fra selve klassen eller fra en subklasse, og ikke direkte
    /// fra en testklasse. Denne proxy arver derfor fra kisten og giver små public “wrapper”-metoder,
    /// så testen kan kalde logikken uden at ændre originale scripts.
    /// </para>
    /// <para>
    /// Proxy’en ændrer ikke adfærden i spillet. Den er kun et hjælpe-objekt i tests, som gør det muligt
    /// at teste de beskyttede metoder på en enkel og tydelig måde.
    /// </para>
    private class FirstChestProxy : FirstChest
    {
        public bool CallCanOpen() => base.CanOpen();
        public IChestLoot CallCreateLoot() => base.CreateLoot();
    }

    /// <summary>
    /// Opretter en Player med tag og nødvendige komponenter, så Chest.Awake kan finde den.
    /// </summary>
    [SetUp]
    public void SetUp()
    {
        playerGo = new GameObject("Player");
        playerGo.tag = "Player";
        playerGo.AddComponent<PlayerInventory>();
        playerGo.AddComponent<PlayerWeapon>();
    }

    /// <summary>
    /// Rydder op efter hver test.
    /// </summary>
    [TearDown]
    public void TearDown()
    {
        if (playerGo != null)
            UnityEngine.Object.DestroyImmediate(playerGo);
    }

    /// <summary>
    /// Verificerer at CanOpen returnerer true, når requiredMonster ikke er sat.
    /// </summary>
    [Test]
    public void CanOpen_ReturnsTrue_WhenRequiredMonsterIsNull()
    {
        FirstChestProxy chest = CreateChest();

        Assert.IsTrue(chest.CallCanOpen());
    }

    /// <summary>
    /// Verificerer at CanOpen returnerer true, når monsteret er besejret.
    /// </summary>
    [Test]
    public void CanOpen_ReturnsTrue_WhenMonsterDefeated()
    {
        FirstChestProxy chest = CreateChest();

        Monster monster = CreateConcreteMonsterOrInconclusive();
        SetMonsterHealth(monster, 0);
        SetPrivateField(typeof(Chest), chest, "requiredMonster", monster);

        Assert.IsTrue(chest.CallCanOpen());

        UnityEngine.Object.DestroyImmediate(monster.gameObject);
    }

    /// <summary>
    /// Verificerer at CanOpen returnerer false, når monsteret stadig lever.
    /// </summary>
    [Test]
    public void CanOpen_ReturnsFalse_WhenMonsterAlive()
    {
        FirstChestProxy chest = CreateChest();

        Monster monster = CreateConcreteMonsterOrInconclusive();
        SetMonsterHealth(monster, 10);
        SetPrivateField(typeof(Chest), chest, "requiredMonster", monster);

        Assert.IsFalse(chest.CallCanOpen());

        UnityEngine.Object.DestroyImmediate(monster.gameObject);
    }

    /// <summary>
    /// Verificerer at CreateLoot logger fejl og returnerer null, når våben mangler.
    /// </summary>
    [Test]
    public void CreateLoot_LogsError_AndReturnsNull_WhenWeaponMissing()
    {
        FirstChestProxy chest = CreateChest();
        SetPrivateField(typeof(FirstChest), chest, "axeWeaponPrefab", null);

        UnityEngine.TestTools.LogAssert.Expect(LogType.Error, "FirstChest: axeWeaponPrefab er ikke sat i Inspector.");
        IChestLoot loot = chest.CallCreateLoot();

        Assert.IsNull(loot);
    }

    /// <summary>
    /// Verificerer at CreateLoot returnerer loot, når våben er sat.
    /// </summary>
    [Test]
    public void CreateLoot_ReturnsLoot_WhenWeaponAssigned()
    {
        FirstChestProxy chest = CreateChest();

        Weapon axe = new GameObject("Axe").AddComponent<Axe>();
        SetPrivateField(typeof(FirstChest), chest, "axeWeaponPrefab", axe);

        IChestLoot loot = chest.CallCreateLoot();
        Assert.IsNotNull(loot);

        UnityEngine.Object.DestroyImmediate(axe.gameObject);
    }

    /// <summary>
    /// Verificerer at looten giver keyId=1 og potionAmount=1 til inventory.
    /// </summary>
    [Test]
    public void Loot_GivesExpectedKeyAndPotions()
    {
        FirstChestProxy chest = CreateChest();

        Weapon axe = new GameObject("Axe").AddComponent<Axe>();
        SetPrivateField(typeof(FirstChest), chest, "axeWeaponPrefab", axe);

        IChestLoot loot = chest.CallCreateLoot();
        Assert.IsNotNull(loot);

        PlayerInventory inventory = playerGo.GetComponent<PlayerInventory>();
        int before = inventory.GetPotionCount();

        loot.GiveItemsToPlayer(inventory);

        Assert.IsTrue(inventory.HasKey(1));
        Assert.AreEqual(before + 1, inventory.GetPotionCount());

        UnityEngine.Object.DestroyImmediate(axe.gameObject);
    }

    /// <summary>
    /// Opretter en kiste på et GameObject med BoxCollider og child Animator.
    /// </summary>
    private static FirstChestProxy CreateChest()
    {
        GameObject chestGo = new GameObject("FirstChest");
        chestGo.AddComponent<BoxCollider>();

        GameObject animChild = new GameObject("AnimatorChild");
        animChild.transform.SetParent(chestGo.transform);
        animChild.AddComponent<Animator>();

        return chestGo.AddComponent<FirstChestProxy>();
    }

    /// <summary>
    /// Finder en ikke-abstract subtype af Monster og tilføjer den som component.
    /// </summary>
    private static Monster CreateConcreteMonsterOrInconclusive()
    {
        Type monsterType = typeof(Monster);

        Type concrete = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a =>
            {
                try { return a.GetTypes(); }
                catch (ReflectionTypeLoadException e) { return e.Types.Where(t => t != null); }
            })
            .FirstOrDefault(t => monsterType.IsAssignableFrom(t) && !t.IsAbstract);

        if (concrete == null)
            Assert.Inconclusive("Ingen konkret Monster-subtype blev fundet i projektet.");

        GameObject go = new GameObject("TestMonster");
        Component c = go.AddComponent(concrete);
        return (Monster)c;
    }

    /// <summary>
    /// Sætter Monster.CurrentHealth via reflection, uanset om det er property eller felt.
    /// </summary>
    private static void SetMonsterHealth(Monster monster, int value)
    {
        Type t = monster.GetType();

        PropertyInfo p = t.GetProperty("CurrentHealth", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (p != null && p.CanWrite)
        {
            p.SetValue(monster, value);
            return;
        }

        FieldInfo f =
            t.GetField("CurrentHealth", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance) ??
            t.GetField("currentHealth", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance) ??
            t.GetField("_currentHealth", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        Assert.IsNotNull(f, "Kunne ikke sætte CurrentHealth på monsteret via reflection.");
        f.SetValue(monster, value);
    }

    /// <summary>
    /// Sætter private fields via reflection.
    /// </summary>
    private static void SetPrivateField(Type declaringType, object target, string fieldName, object value)
    {
        FieldInfo field = declaringType.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(field, $"{declaringType.Name}.{fieldName} blev ikke fundet via reflection.");
        field.SetValue(target, value);
    }
}
