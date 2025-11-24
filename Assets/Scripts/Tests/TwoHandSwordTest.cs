using NUnit.Framework;
using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.TestTools;

public class TwoHandSwordTest
{
    /// <summary>
    /// Tester at TwoHandSword.CalculateDamage returnerer 10 skade.
    /// </summary>
    [Test]
    public void TwoHandSword_CalculateDamage_Returns10()
    {
        var gameObject = new GameObject();
        var twoHandSword = gameObject.AddComponent<TwoHandSword>();

        int damage = twoHandSword.CalculateDamage();


        Assert.AreEqual(30, damage);
    }
    /// <summary>
    /// Sikrer at TwoHandSword.CalculateDamage aldrig returnerer negativ skade.
    /// </summary>
    [Test]
    public void TwoHandSword_CalculateDamage_IsNeverNegative()
    {
        var go = new GameObject();
        var sword = go.AddComponent<TwoHandSword>();

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

        var TwoHandSwordGameObject = new GameObject("TwoHandSword");
        var TwoHandSword = TwoHandSwordGameObject.AddComponent<TwoHandSword>();


        var MonsterGameObject = new GameObject("Monster");
        var MonsterBoxCollider = MonsterGameObject.AddComponent<BoxCollider>();
        var Monster = MonsterGameObject.AddComponent<Monster>();

        Monster.CurrentHealth = 100;
        int StartHealth = Monster.CurrentHealth;

        TwoHandSword.Attack(Monster);

        Assert.AreEqual(StartHealth - 30, Monster.CurrentHealth);
    }

    /// <summary>
    /// Sikrer at Attack ikke skader et monster, når våbnet ikke angriber.
    /// </summary>
    [Test]
    public void Attack_DoesNotDamage_WhenNotAttacking()
    {
        var TwoHandSwordGameObject = new GameObject("TwoHandSword");
        var TwoHandSword = TwoHandSwordGameObject.AddComponent<TwoHandSword>();
    

        var MonsterGameObject = new GameObject("Monster");
        var MonsterBoxCollider = MonsterGameObject.AddComponent<BoxCollider>();
        var Monster = MonsterGameObject.AddComponent<Monster>();

        Monster.CurrentHealth = 100;
        int StartHealth = Monster.CurrentHealth;

        TwoHandSword.Attack(Monster);


        Assert.AreEqual(StartHealth, Monster.CurrentHealth);
    }

    /// <summary>
    /// Sikrer at Attack ikke skader et monster, der allerede er dødt (0 HP).
    /// </summary>
    [Test]
    public void Attack_DoesNotDamage_DeadMonster()
    {
        var TwoHandSwordGameObject = new GameObject("TwoHandSword");
        var TwoHandSword = TwoHandSwordGameObject.AddComponent<TwoHandSword>();


        var MonsterGameObject = new GameObject("Monster");
        var MonsterBoxCollider = MonsterGameObject.AddComponent<BoxCollider>();
        var Monster = MonsterGameObject.AddComponent<Monster>();

        Monster.CurrentHealth = 0;

        TwoHandSword.Attack(Monster);

        Assert.AreEqual(0, Monster.CurrentHealth);
    }
}

