using System.Collections;
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
    /// GameObject der bruges som test-player i de fleste tests.
    /// </summary>
    private GameObject playerObject;

    /// <summary>
    /// PlayerWeapon-komponenten, som der testes på.
    /// </summary>
    private PlayerWeapon playerWeapon;

    /// <summary>
    /// Kører før hver test.
    /// Opretter en Player med PlayerWeapon og en WeaponPivot-socket.
    /// </summary>
    [SetUp]
    public void Setup()
    {
        playerObject = new GameObject("Player");
        playerWeapon = playerObject.AddComponent<PlayerWeapon>();

        var socketGO = new GameObject("WeaponPivot");
        socketGO.transform.SetParent(playerObject.transform, false);

        var socketField = typeof(PlayerWeapon).GetField(
            "weaponSocketTransform",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic
        );
        socketField.SetValue(playerWeapon, socketGO.transform);
    }

    /// <summary>
    /// Rydder op efter hver test ved at destruere player-objektet.
    /// </summary>
    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(playerObject);
    }

    /// <summary>
    /// Sikrer at EquipNewWeapon(null) ikke ændrer det eksisterende våben.
    /// </summary>
    [Test]
    public void EquipNewWeapon_WithNull_DoesNotChangeExistingWeapon()
    {
        var socketTransform = playerWeapon.transform.Find("WeaponPivot");
        var oldWeaponGO = new GameObject("OldWeapon");
        oldWeaponGO.transform.SetParent(socketTransform, false);
        var oldWeapon = oldWeaponGO.AddComponent<OneHandSword>();

        var equippedField = typeof(PlayerWeapon).GetField(
            "equippedWeaponComponent",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic
        );
        equippedField.SetValue(playerWeapon, oldWeapon);

        playerWeapon.EquipNewWeapon(null);

        var after = (OneHandSword)equippedField.GetValue(playerWeapon);
        Assert.AreSame(oldWeapon, after);
    }

    /// <summary>
    /// Tester at GetClosestMonsterInAttackRange returnerer null,
    /// når der ikke er nogen monstre inden for rækkevidde.
    /// </summary>
    [Test]
    public void GetClosestMonsterInAttackRange_ReturnsNull_WhenNoMonsters()
    {
        var method = typeof(PlayerWeapon).GetMethod(
            "GetClosestMonsterInAttackRange",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic
        );

        var result = method.Invoke(playerWeapon, null) as Monster;

        Assert.IsNull(result);
    }

 
    /// <summary>
    /// Tester at GetClosestMonsterInAttackRange returnerer det nærmeste monster,
    /// når flere monstre er inden for angrebsområdet.
    /// </summary>
    [Test]
    public void GetClosestMonsterInAttackRange_Returns_Closest_Monster()
    {
        var maskField = typeof(PlayerWeapon).GetField(
            "monsterLayerMask",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic
        );
        var mask = (LayerMask)maskField.GetValue(playerWeapon);
        mask.value = ~0;
        maskField.SetValue(playerWeapon, mask);

        playerObject.transform.position = Vector3.zero;
        playerObject.transform.forward = Vector3.forward;

       
        var m1GO = new GameObject("Monster1");
        m1GO.transform.position = new Vector3(0f, 0f, 2f);
        m1GO.AddComponent<SphereCollider>();
        var m1 = m1GO.AddComponent<Dragon>();

        var m2GO = new GameObject("Monster2");
        m2GO.transform.position = new Vector3(0f, 0f, 2.5f);
        m2GO.AddComponent<SphereCollider>();
        m2GO.AddComponent<Dragon>();

        var method = typeof(PlayerWeapon).GetMethod(
            "GetClosestMonsterInAttackRange",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic
        );
        var result = method.Invoke(playerWeapon, null) as Monster;

        Assert.AreSame(m1, result);

        Object.DestroyImmediate(m1GO);
        Object.DestroyImmediate(m2GO);
    }
   
    /// <summary>
    /// Tester at SetupWeaponSocketTransform vælger et child med navnet "WeaponPivot"
    /// når et sådant eksisterer.
    /// </summary>
    [Test]
    public void SetupWeaponSocketTransform_UsesChild_WhenChildExists()
    {
        var tempGO = new GameObject("TempPlayer");
        var tempWeapon = tempGO.AddComponent<PlayerWeapon>();

        var childSocketGO = new GameObject("WeaponPivot");
        childSocketGO.transform.SetParent(tempGO.transform, false);

        var socketField = typeof(PlayerWeapon).GetField(
            "weaponSocketTransform",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic
        );
        socketField.SetValue(tempWeapon, null);

        var method = typeof(PlayerWeapon).GetMethod(
            "SetupWeaponSocketTransform",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic
        );
        method.Invoke(tempWeapon, null);

        var value = (Transform)socketField.GetValue(tempWeapon);
        Assert.AreEqual(childSocketGO.transform, value);

        Object.DestroyImmediate(tempGO);
    }

    /// <summary>
    /// Tester at SetupWeaponSocketTransform falder tilbage til objektets egen transform,
    /// når der ikke eksisterer et child med navnet "WeaponPivot".
    /// </summary>
    [Test]
    public void SetupWeaponSocketTransform_FallsBack_ToOwnTransform_WhenNoChild()
    {
        var tempGO = new GameObject("TempPlayer_NoChild");
        var tempWeapon = tempGO.AddComponent<PlayerWeapon>();

        var socketField = typeof(PlayerWeapon).GetField(
            "weaponSocketTransform",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic
        );
        socketField.SetValue(tempWeapon, null);

        var method = typeof(PlayerWeapon).GetMethod(
            "SetupWeaponSocketTransform",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic
        );
        method.Invoke(tempWeapon, null);

        var value = (Transform)socketField.GetValue(tempWeapon);
        Assert.AreEqual(tempGO.transform, value);

        Object.DestroyImmediate(tempGO);
    }

    /// <summary>
    /// Tester at SetupMonsterLayerMask sætter en gyldig mask,
    /// når den oprindelige værdi er 0.
    /// </summary>
    [Test]
    public void SetupMonsterLayerMask_SetsMask_WhenValueIsZero()
    {
        var tempGO = new GameObject("TempPlayer_Mask");
        var tempWeapon = tempGO.AddComponent<PlayerWeapon>();

        var maskField = typeof(PlayerWeapon).GetField(
            "monsterLayerMask",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic
        );

        var mask = (LayerMask)maskField.GetValue(tempWeapon);
        mask.value = 0;
        maskField.SetValue(tempWeapon, mask);

        var method = typeof(PlayerWeapon).GetMethod(
            "SetupMonsterLayerMask",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic
        );
        method.Invoke(tempWeapon, null);

        var after = (LayerMask)maskField.GetValue(tempWeapon);
        Assert.AreNotEqual(0, after.value);

        Object.DestroyImmediate(tempGO);
    }
}
