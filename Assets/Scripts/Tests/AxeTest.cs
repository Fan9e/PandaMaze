using NUnit.Framework;
using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.TestTools;

public class AxeTest
{
    /// <summary>
    /// Tester at Axe.CalculateDamage returnerer 15 skade.
    /// </summary>
    [Test]
    public void Axe_CalculateDamage_Returns10()
    {
        var gameObject = new GameObject();
        var axe = gameObject.AddComponent<Axe>();

        int damage = axe.CalculateDamage();


        Assert.AreEqual(15, damage);
    }
    /// <summary>
    /// Sikrer at Axe.CalculateDamage aldrig returnerer negativ skade.
    /// </summary>
    [Test]
    public void Axe_CalculateDamage_IsNeverNegative()
    {
        var go = new GameObject();
        var axe = go.AddComponent<Axe>();

        for (int i = 0; i < 100; i++)
        {
            int damage = axe.CalculateDamage();
            Assert.GreaterOrEqual(damage, 0, "Damage må ikke være negativ");
        }
    }


    /// <summary>
    /// Sikrer at Attack skader et monster, når våbnet angriber og monsteret er i live.
    /// </summary>
    [Test]
    public void Attack_DamagesMonster_WhenAttackingAndMonsterAlive()
    {

        var AxeGameObject = new GameObject("Axe");
        var Axe = AxeGameObject.AddComponent<Axe>();

        Axe.isAttacking = true;

        var MonsterGameObject = new GameObject("Monster");
        var MonsterBoxCollider = MonsterGameObject.AddComponent<BoxCollider>();
        var Monster = MonsterGameObject.AddComponent<Monster>();

        Monster.CurrentHealth = 100;
        int StartHealth = Monster.CurrentHealth;

        Axe.Attack(Monster);

        Assert.AreEqual(StartHealth - 15, Monster.CurrentHealth);
    }

    /// <summary>
    /// Sikrer at Attack ikke skader et monster, når våbnet ikke angriber.
    /// </summary>
    [Test]
    public void Attack_DoesNotDamage_WhenNotAttacking()
    {
        var AxeGameObject = new GameObject("Axe");
        var Axe = AxeGameObject.AddComponent<Axe>();
        Axe.isAttacking = false;

        var MonsterGameObject = new GameObject("Monster");
        var MonsterBoxCollider = MonsterGameObject.AddComponent<BoxCollider>();
        var Monster = MonsterGameObject.AddComponent<Monster>();

        Monster.CurrentHealth = 100;
        int StartHealth = Monster.CurrentHealth;

        Axe.Attack(Monster);


        Assert.AreEqual(StartHealth, Monster.CurrentHealth);
    }

    /// <summary>
    /// Sikrer at Attack ikke skader et monster, der allerede er dødt (0 HP).
    /// </summary>
    [Test]
    public void Attack_DoesNotDamage_DeadMonster()
    {
        var AxeGameObject = new GameObject("Axe");
        var Axe = AxeGameObject.AddComponent<Axe>();
        Axe.isAttacking = true;

        var MonsterGameObject = new GameObject("Monster");
        var MonsterBoxCollider = MonsterGameObject.AddComponent<BoxCollider>();
        var Monster = MonsterGameObject.AddComponent<Monster>();

        Monster.CurrentHealth = 0;

        Axe.Attack(Monster);

        Assert.AreEqual(0, Monster.CurrentHealth);
    }
}

