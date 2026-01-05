using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// Tests for PlayerWeapon-komponenten.
/// Verificerer våben-equipping, monster-detection og setup-metoder.
/// </summary>
public class PlayerWeaponTests
{
    /// <summary>
    /// Hjælpe-konstanter til reflection flags.
    /// </summary>
    private const BindingFlags PrivateInstanceFlags = BindingFlags.Instance | BindingFlags.NonPublic;

    /// <summary>
    /// Rydder op efter hver test, hvis der blev oprettet objekter, som ikke blev fjernet.
    /// </summary>
    [TearDown]
    public void TearDown()
    {
        // Find alle root-objekter og slet dem, der matcher vores test-navne.
        // (Det gør testen mere robust, hvis en test fejler før DestroyImmediate.)
        foreach (var root in UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
        {
            if (root == null) continue;

            if (root.name.StartsWith("Test_", System.StringComparison.Ordinal))
            {
                Object.DestroyImmediate(root);
            }
        }
    }

    /// <summary>
    /// Tester at EnsureWeaponSocket sætter weaponSocketTransform til et child,
    /// når child-objektet "WeaponPivot" findes.
    /// </summary>
    [Test]
    public void EnsureWeaponSocket_SetsSocketToChild_WhenWeaponPivotExists()
    {
        var playerObject = new GameObject("Test_Player");
        var weaponPivot = new GameObject("WeaponPivot");
        weaponPivot.transform.SetParent(playerObject.transform, false);

        var playerWeapon = playerObject.AddComponent<PlayerWeapon>();

        SetPrivateField(playerWeapon, "weaponSocketChildName", "WeaponPivot");
        SetPrivateField(playerWeapon, "weaponSocketTransform", null);

        InvokePrivateMethod(playerWeapon, "EnsureWeaponSocket");

        var socketTransform = GetPrivateField<Transform>(playerWeapon, "weaponSocketTransform");
        Assert.AreEqual(weaponPivot.transform, socketTransform);

        Object.DestroyImmediate(playerObject);
    }

    /// <summary>
    /// Tester at EnsureWeaponSocket falder tilbage til spillerens egen transform,
    /// når der ikke findes et child med navnet "WeaponPivot".
    /// </summary>
    [Test]
    public void EnsureWeaponSocket_FallsBackToOwnTransform_WhenWeaponPivotDoesNotExist()
    {
        var playerObject = new GameObject("Test_Player_NoPivot");
        var playerWeapon = playerObject.AddComponent<PlayerWeapon>();

        SetPrivateField(playerWeapon, "weaponSocketChildName", "WeaponPivot");
        SetPrivateField(playerWeapon, "weaponSocketTransform", null);

        InvokePrivateMethod(playerWeapon, "EnsureWeaponSocket");

        var socketTransform = GetPrivateField<Transform>(playerWeapon, "weaponSocketTransform");
        Assert.AreEqual(playerObject.transform, socketTransform);

       Object.DestroyImmediate(playerObject);
    }

    /// <summary>
    /// Tester at EnsureMonsterLayerMask sætter monsterLayerMask,
    /// når maskens værdi er 0 og layer-navnet er et eksisterende layer.
    /// (Vi bruger "Default", fordi det altid findes i Unity.)
    /// </summary>
    [Test]
    public void EnsureMonsterLayerMask_SetsMask_WhenValueIsZero_AndLayerExists()
    {
        var playerObject = new GameObject("Test_Player_Mask");
        var playerWeapon = playerObject.AddComponent<PlayerWeapon>();

        SetPrivateField(playerWeapon, "monsterLayerName", "Default");

        var mask = GetPrivateField<LayerMask>(playerWeapon, "monsterLayerMask");
        mask.value = 0;
        SetPrivateField(playerWeapon, "monsterLayerMask", mask);

        InvokePrivateMethod(playerWeapon, "EnsureMonsterLayerMask");

        var after = GetPrivateField<LayerMask>(playerWeapon, "monsterLayerMask");
        Assert.AreNotEqual(0, after.value);

        Object.DestroyImmediate(playerObject);
    }

    /// <summary>
    /// Tester at EquipNewWeapon(null) ikke ændrer det eksisterende weapon component.
    /// </summary>
    [Test]
    public void EquipNewWeapon_WithNull_DoesNotChangeExistingWeaponComponent()
    {
        var playerObject = new GameObject("Test_Player_EquipNull");
        var weaponPivot = new GameObject("WeaponPivot");
        weaponPivot.transform.SetParent(playerObject.transform, false);

        var playerWeapon = playerObject.AddComponent<PlayerWeapon>();

        // VIGTIGT:
        // Denne test forudsætter at du har en konkret Weapon-type i dit projekt, fx OneHandSword.
        // Hvis du ikke har OneHandSword, så udskift den med din egen konkrete Weapon-klasse.
        var oldWeaponObject = new GameObject("Test_OldWeapon");
        oldWeaponObject.transform.SetParent(weaponPivot.transform, false);
        var oldWeapon = oldWeaponObject.AddComponent<OneHandSword>();

        SetPrivateField(playerWeapon, "equippedWeaponComponent", oldWeapon);

        playerWeapon.EquipNewWeapon(null);

        var after = GetPrivateField<Weapon>(playerWeapon, "equippedWeaponComponent");
        Assert.AreSame(oldWeapon, after);

        Object.DestroyImmediate(playerObject);
    }

    /// <summary>
    /// Tester at EnsureWeaponAnimator finder en Animator under weapon socket,
    /// når weaponAnimator ikke allerede er sat.
    /// </summary>
    [Test]
    public void EnsureWeaponAnimator_FindsAnimatorInWeaponPivotChildren()
    {
        var playerObject = new GameObject("Test_Player_Animator");
        var weaponPivot = new GameObject("WeaponPivot");
        weaponPivot.transform.SetParent(playerObject.transform, false);

        var animatorHolder = new GameObject("AnimatorHolder");
        animatorHolder.transform.SetParent(weaponPivot.transform, false);
        var animator = animatorHolder.AddComponent<Animator>();

        var playerWeapon = playerObject.AddComponent<PlayerWeapon>();

        SetPrivateField(playerWeapon, "weaponSocketTransform", weaponPivot.transform);
        SetPrivateField(playerWeapon, "weaponAnimator", null);

        InvokePrivateMethod(playerWeapon, "EnsureWeaponAnimator");

        var foundAnimator = GetPrivateField<Animator>(playerWeapon, "weaponAnimator");
        Assert.AreSame(animator, foundAnimator);

        UnityEngine.Object.DestroyImmediate(playerObject);
    }

    /// <summary>
    /// Tester at EnsureWeaponEquipped:
    /// Finder et Weapon i child-objekter
    /// Flytter våbnet til weaponSocketTransform hvis det ikke allerede sidder der
    /// Sætter EquippedWeaponInterface (forudsætter at våbnet implementerer IWeapon)
    /// </summary>
    [Test]
    public void EnsureWeaponEquipped_FindsWeaponAndParentsItToSocket_AndSetsInterface()
    {
        var playerObject = new GameObject("Test_Player_EnsureWeapon");
        var weaponPivot = new GameObject("WeaponPivot");
        weaponPivot.transform.SetParent(playerObject.transform, false);

        // Læg våbnet et andet sted end socket, så vi kan se at det bliver flyttet.
        var weaponHolder = new GameObject("WeaponHolder");
        weaponHolder.transform.SetParent(playerObject.transform, false);

        var weaponObject = new GameObject("Test_Weapon");
        weaponObject.transform.SetParent(weaponHolder.transform, false);
        var weapon = weaponObject.AddComponent<OneHandSword>();

        var playerWeapon = playerObject.AddComponent<PlayerWeapon>();

        SetPrivateField(playerWeapon, "weaponSocketTransform", weaponPivot.transform);
        SetPrivateField(playerWeapon, "equippedWeaponComponent", null);

        InvokePrivateMethod(playerWeapon, "EnsureWeaponEquipped");

        var equippedWeaponComponent = GetPrivateField<Weapon>(playerWeapon, "equippedWeaponComponent");
        Assert.AreSame(weapon, equippedWeaponComponent);
        Assert.AreEqual(weaponPivot.transform, weapon.transform.parent);

        var equippedInterface = GetPublicProperty<object>(playerWeapon, "EquippedWeaponInterface");
        Assert.IsNotNull(equippedInterface, "EquippedWeaponInterface skal være sat, hvis våbnet implementerer IWeapon.");

        Object.DestroyImmediate(playerObject);
    }

    [Test]
    public void AttackSpecificMonster_DoesNothing_WhenNoWeaponOrNullMonster()
    {
        var player = new GameObject("Test_Player_Attack");
        var weaponPivot = new GameObject("WeaponPivot");
        weaponPivot.transform.SetParent(player.transform, false);
        var pw = player.AddComponent<PlayerWeapon>();
        SetPrivateField(pw, "weaponSocketTransform", weaponPivot.transform);

        // Case 1: null monster
        pw.AttackSpecificMonster(null);
        Assert.IsNull(GetPrivateField<Coroutine>(pw, "attackRoutine"));

        // Case 2: no weapon
        var dummyMonster = new GameObject("Monster").AddComponent<Dragon>();
        pw.AttackSpecificMonster(dummyMonster);
        Assert.IsNull(GetPrivateField<Coroutine>(pw, "attackRoutine"));

        Object.DestroyImmediate(player);
        Object.DestroyImmediate(dummyMonster.gameObject);
    }
    private class FakeWeapon : Weapon, IWeapon
    {
        public bool attacked;
        public string AttackAnimationName => "";
        public void Attack(Monster m) => attacked = true;

        public override int CalculateDamage()
        {
            return 12;
        }
    }

    [UnityTest]
    public IEnumerator AttackSpecificMonster_StartsCoroutine_WhenValid()
    {
        var player = new GameObject("Test_Player_Attack");
        var pivot = new GameObject("WeaponPivot");
        pivot.transform.SetParent(player.transform, false);

        var pw = player.AddComponent<PlayerWeapon>();
        SetPrivateField(pw, "weaponSocketTransform", pivot.transform);

        var fakeW = new GameObject("FakeWeapon").AddComponent<FakeWeapon>();
        fakeW.transform.SetParent(pivot.transform, false);
        SetPrivateField(pw, "equippedWeaponComponent", fakeW);
        SetPublicField(pw, "EquippedWeaponInterface", fakeW);

        var monster = new GameObject("Monster").AddComponent<Dragon>();

        pw.AttackSpecificMonster(monster);

        yield return null;

        Assert.IsTrue(fakeW.attacked, "Weapon.Attack() skulle være kaldt");
        Assert.IsFalse(GetPrivateField<bool>(pw, "isCurrentlyAttacking"), "flag skulle nulstilles til false");

        Object.DestroyImmediate(player);
        Object.DestroyImmediate(monster.gameObject);
    }


    [Test]
    public void EquipWeaponInternal_DestroysOldWeapon_WhenDestroyOldTrue()
    {
        var player = new GameObject("Test_Player_EquipInternal");
        var pivot = new GameObject("WeaponPivot");
        pivot.transform.SetParent(player.transform, false);
        var pw = player.AddComponent<PlayerWeapon>();

        SetPrivateField(pw, "weaponSocketTransform", pivot.transform);

        var oldW = new GameObject("OldWeapon").AddComponent<OneHandSword>();
        SetPrivateField(pw, "equippedWeaponComponent", oldW);

        var method = typeof(PlayerWeapon).GetMethod("EquipWeaponInternal", BindingFlags.Instance | BindingFlags.NonPublic);
        var prefab = new GameObject("PrefabWeapon").AddComponent<OneHandSword>();

        method.Invoke(pw, new object[] { prefab, true });

        var equipped = GetPrivateField<Weapon>(pw, "equippedWeaponComponent");
        Assert.IsNotNull(equipped);
        Assert.AreNotSame(oldW, equipped);
    }

    [Test]
    public void EnsureWeaponEquipped_LogsError_WhenNoWeaponAndNoPrefab()
    {
        var player = new GameObject("Test_Player_NoWeapon");
        var pivot = new GameObject("WeaponPivot");
        pivot.transform.SetParent(player.transform, false);
        var pw = player.AddComponent<PlayerWeapon>();
        SetPrivateField(pw, "weaponSocketTransform", pivot.transform);
        SetPrivateField(pw, "startingWeaponPrefab", null);

        LogAssert.ignoreFailingMessages = false;
        LogAssert.Expect(LogType.Error, "Spilleren kunne hverken finde eller oprette et våben.");

        InvokePrivateMethod(pw, "EnsureWeaponEquipped");

        var eq = GetPublicProperty<IWeapon>(pw, "EquippedWeaponInterface");
        Assert.IsNull(eq);
    }

    /// <summary>
    /// Kalder en private metode på et objekt via reflection.
    /// </summary>
    private static void InvokePrivateMethod(object instance, string methodName)
    {
        var method = instance.GetType().GetMethod(methodName, PrivateInstanceFlags);
        Assert.IsNotNull(method, $"Kunne ikke finde metoden '{methodName}'. Tjek at navnet matcher PlayerWeapon.");
        method.Invoke(instance, null);
    }

    /// <summary>
    /// Henter en private field via reflection.
    /// </summary>
    private static T GetPrivateField<T>(object instance, string fieldName)
    {
        var field = instance.GetType().GetField(fieldName, PrivateInstanceFlags);
        Assert.IsNotNull(field, $"Kunne ikke finde field '{fieldName}'. Tjek at navnet matcher PlayerWeapon.");
        return (T)field.GetValue(instance);
    }

    /// <summary>
    /// Sætter en private field via reflection.
    /// </summary>
    private static void SetPrivateField(object instance, string fieldName, object value)
    {
        var field = instance.GetType().GetField(fieldName, PrivateInstanceFlags);
        Assert.IsNotNull(field, $"Kunne ikke finde field '{fieldName}'. Tjek at navnet matcher PlayerWeapon.");
        field.SetValue(instance, value);
    }

    /// <summary>
    /// Sætter en public property (auto-property) via reflection.
    /// </summary>
    private static void SetPublicField(object instance, string propertyName, object value)
    {
        var property = instance.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
        Assert.IsNotNull(property, $"Kunne ikke finde public property '{propertyName}'.");
        property.SetValue(instance, value);
    }

    /// <summary>
    /// Henter en public property (auto-property) via reflection.
    /// Bruges fx til EquippedWeaponInterface.
    /// </summary>
    private static T GetPublicProperty<T>(object instance, string propertyName)
    {
        var property = instance.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
        Assert.IsNotNull(property, $"Kunne ikke finde public property '{propertyName}'.");
        return (T)property.GetValue(instance);
    }
}
