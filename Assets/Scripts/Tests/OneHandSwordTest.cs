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
    public void OneHandSword_CalculateDamage_Returns10()
    {
        var gameObject = new GameObject();
        var oneHandSword = gameObject.AddComponent<OneHandSword>();
       
        int damage = oneHandSword.CalculateDamage();

       
        Assert.AreEqual(10, damage);
    }
    /// <summary>
    /// Sikrer at OneHandSword.CalculateDamage aldrig returnerer negativ skade.
    /// </summary>
    [Test]
    public void OneHandSword_CalculateDamage_IsNeverNegative()
    {
        var go = new GameObject();
        var sword = go.AddComponent<OneHandSword>();

        for (int i = 0; i < 100; i++)
        {
            int damage = sword.CalculateDamage();
            Assert.GreaterOrEqual(damage, 0, "Damage må ikke være negativ");
        }
    }


    /// <summary>
    /// Sikrer at Attack skader et monster, når våbnet angriber og monsteret er i live.
    /// </summary>
    [Test]
    public void Attack_DamagesMonster_WhenAttackingAndMonsterAlive()
    {
    
        var OneHandSwordGameObject = new GameObject("OneHandSword");
        var OneHandSword = OneHandSwordGameObject.AddComponent<OneHandSword>();

        OneHandSword.isAttacking = true;

        var MonsterGameObject = new GameObject("Monster");
        var MonsterBoxCollider = MonsterGameObject.AddComponent<BoxCollider>();
        var Monster = MonsterGameObject.AddComponent<Monster>();

        Monster.CurrentHealth = 100;
        int StartHealth = Monster.CurrentHealth;

        OneHandSword.Attack(Monster);

        Assert.AreEqual(StartHealth - 10, Monster.CurrentHealth);
    }

    /// <summary>
    /// Sikrer at Attack ikke skader et monster, når våbnet ikke angriber.
    /// </summary>
    [Test]
    public void Attack_DoesNotDamage_WhenNotAttacking()
    { 
        var OneHandSwordGameObject = new GameObject("OneHandSword");
        var OneHandSword = OneHandSwordGameObject.AddComponent<OneHandSword>();
        OneHandSword.isAttacking = false;   

        var MonsterGameObject = new GameObject("Monster");
        var MonsterBoxCollider = MonsterGameObject.AddComponent<BoxCollider>();
        var Monster = MonsterGameObject.AddComponent<Monster>();

        Monster.CurrentHealth = 100;
        int StartHealth = Monster.CurrentHealth;

        OneHandSword.Attack(Monster);


        Assert.AreEqual(StartHealth, Monster.CurrentHealth);
    }

    /// <summary>
    /// Sikrer at Attack ikke skader et monster, der allerede er dødt (0 HP).
    /// </summary>
    [Test]
    public void Attack_DoesNotDamage_DeadMonster()
    {
        var OneHandSwordGameObject = new GameObject("OneHandSword");
        var OneHandSword = OneHandSwordGameObject.AddComponent<OneHandSword>();
        OneHandSword.isAttacking = true;

        var MonsterGameObject = new GameObject("Monster");
        var MonsterBoxCollider = MonsterGameObject.AddComponent<BoxCollider>();
        var Monster = MonsterGameObject.AddComponent<Monster>();

        Monster.CurrentHealth = 0;

        OneHandSword.Attack(Monster);

        Assert.AreEqual(0, Monster.CurrentHealth);
    }
}

