using NUnit.Framework;
using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.TestTools;

public class OneHandSwordTest
{
    /// <summary>
    /// Tester at OneHandSword.CalculateDamage returnerer 10 skade.
    /// </summary>
    [Test]
    public void OneHandSword_Attack_Returns10()
    {
        var gameObject = new GameObject();
        var oneHandSword = gameObject.AddComponent<OneHandSword>();
       
        int damage = oneHandSword.CalculateDamage();

       
        Assert.AreEqual(10, damage);
    }

    /// <summary>
    /// Sikrer at OnTriggerStay skader et monster, når våbnet angriber og monsteret er i live.
    /// </summary>
    [Test]
    public void OnTriggerStay_DamagesMonster_WhenAttackingAndMonsterAlive()
    {
    
        var OneHandSwordGameObject = new GameObject("OneHandSword");
        var OneHandSword = OneHandSwordGameObject.AddComponent<OneHandSword>();

        OneHandSword.isAttacking = true;

        var MonsterGameObject = new GameObject("Monster");
        var MonsterBoxCollider = MonsterGameObject.AddComponent<BoxCollider>();
        var Monster = MonsterGameObject.AddComponent<Monster>();

        Monster.CurrentHealth = 100;
        int StartHealth = Monster.CurrentHealth;

      
        var onTriggerStay = typeof(Weapon).GetMethod(
            "OnTriggerStay",
            BindingFlags.Instance | BindingFlags.NonPublic
        );
        Assert.IsNotNull(onTriggerStay, "Kunne ikke finde Weapon.OnTriggerStay");

        onTriggerStay.Invoke(OneHandSword, new object[] { MonsterBoxCollider });

        Assert.AreEqual(StartHealth - 10, Monster.CurrentHealth);
    }

    /// <summary>
    /// Sikrer at OnTriggerStay ikke skader et monster, når våbnet ikke angriber.
    /// </summary>
    [Test]
    public void OnTriggerStay_DoesNotDamage_WhenNotAttacking()
    { 
        var OneHandSwordGameObject = new GameObject("OneHandSword");
        var OneHandSword = OneHandSwordGameObject.AddComponent<OneHandSword>();
        OneHandSword.isAttacking = false;   

        var MonsterGameObject = new GameObject("Monster");
        var MonsterBoxCollider = MonsterGameObject.AddComponent<BoxCollider>();
        var Monster = MonsterGameObject.AddComponent<Monster>();

        Monster.CurrentHealth = 100;
        int StartHealth = Monster.CurrentHealth;

        var onTriggerStay = typeof(Weapon).GetMethod(
            "OnTriggerStay",
            BindingFlags.Instance | BindingFlags.NonPublic
        );

        onTriggerStay.Invoke(OneHandSword, new object[] { MonsterBoxCollider });

 
        Assert.AreEqual(StartHealth, Monster.CurrentHealth);
    }

    /// <summary>
    /// Sikrer at OnTriggerStay ikke skader et monster, der allerede er dødt (0 HP).
    /// </summary>
    [Test]
    public void OnTriggerStay_DoesNotDamage_DeadMonster()
    {
        var OneHandSwordGameObject = new GameObject("OneHandSword");
        var OneHandSword = OneHandSwordGameObject.AddComponent<OneHandSword>();
        OneHandSword.isAttacking = true;

        var MonsterGameObject = new GameObject("Monster");
        var MonsterBoxCollider = MonsterGameObject.AddComponent<BoxCollider>();
        var Monster = MonsterGameObject.AddComponent<Monster>();

        Monster.CurrentHealth = 0;  

        var onTriggerStay = typeof(Weapon).GetMethod(
            "OnTriggerStay",
            BindingFlags.Instance | BindingFlags.NonPublic
        );

        onTriggerStay.Invoke(OneHandSword, new object[] { MonsterBoxCollider });

        Assert.AreEqual(0, Monster.CurrentHealth);
    }
}

