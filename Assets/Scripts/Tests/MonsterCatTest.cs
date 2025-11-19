using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class MonsterCatTest
{
    [Test]
    public void Fight_MonsterTakesDamage_AndDealsDamageToPlayer()
    {

        var monsterGameObject = new GameObject();
        var monster = monsterGameObject.AddComponent<Dragon>();

        var playerGameObject = new GameObject();
        var player = playerGameObject.AddComponent<Player>();

        monster.MaxHealth = 60;
        monster.CurrentHealth = 60;
        monster.AttackPower = 20;
        player.MaxHealth = 100;
        player.CurrentHealth = 100;
        monster.Player = player;
        monster.Fight(7);

        Assert.AreEqual(53, monster.CurrentHealth);
        Assert.AreEqual(80, player.CurrentHealth);
    }
    [Test]
    public void Fight_MonsterDies_WhenHealthReachesZero()
    {

        var monsterGameObject = new GameObject();
        var monster = monsterGameObject.AddComponent<Dragon>();
        var playerGameObject = new GameObject();
        var player = playerGameObject.AddComponent<Player>();

        monster.MaxHealth = 50;
        monster.CurrentHealth = 20;
        monster.AttackPower = 20;
        player.MaxHealth = 30;
        player.CurrentHealth = 60;
        monster.Player = player;
        monster.Fight(20);

        Assert.AreEqual(0, monster.CurrentHealth);

    }
    [Test]
    public void Fight_NegativeDamage_DoesNotChangeHealth()
    {

        var monsterGameObject = new GameObject();
        var monster = monsterGameObject.AddComponent<Dragon>();
        var playerGameObject = new GameObject();
        var player = playerGameObject.AddComponent<Player>();
        monster.MaxHealth = 20;
        monster.CurrentHealth = 20;
        monster.AttackPower = 20;
        monster.Player = player;
        monster.Fight(-5);

        Assert.AreEqual(20, monster.CurrentHealth);
    }
    [Test]
    public void Fight_DoesNothing_IfNoPlayerAssigned()
    {

        var monsterGameObject = new GameObject();
        var monster = monsterGameObject.AddComponent<Dragon>();
        monster.MaxHealth = 20;
        monster.CurrentHealth = 20;
        monster.AttackPower = 20;
        monster.Player = null;
        monster.Fight(5);

        Assert.AreEqual(20, monster.CurrentHealth);
    }
    [Test]
    public void Fight_DoesNothing_IfMonsterAlreadyDead()
    {

        var monsterGameObject = new GameObject();
        var monster = monsterGameObject.AddComponent<Dragon>();
        var playerGameObject = new GameObject();
        var player = playerGameObject.AddComponent<Player>();

        monster.MaxHealth = 10;
        monster.CurrentHealth = 0;
        monster.AttackPower = 20;
        player.MaxHealth = 30;
        player.CurrentHealth = 30;
        monster.Player = player;
        monster.Fight(5);

        Assert.AreEqual(0, monster.CurrentHealth);
        Assert.AreEqual(30, player.CurrentHealth);
    }
    [Test]
    public void Fight_DoesNothing_IfMonsterDiesFromDamage()
    {

        var monsterGameObject = new GameObject();
        var monster = monsterGameObject.AddComponent<Dragon>();
        var playerGameObject = new GameObject();
        var player = playerGameObject.AddComponent<Player>();

        monster.MaxHealth = 10;
        monster.CurrentHealth = 10;
        monster.AttackPower = 20;
        player.MaxHealth = 30;
        player.CurrentHealth = 30;
        monster.Player = player;
        monster.Fight(15);

        Assert.AreEqual(0, monster.CurrentHealth);
        Assert.AreEqual(30, player.CurrentHealth);
    }
}
