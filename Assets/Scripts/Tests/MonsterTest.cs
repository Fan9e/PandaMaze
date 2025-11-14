using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class MonsterTest
{
    [Test]
    public void Fight_MonsterTakesDamage_AndDealsDamageToPlayer()
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
        monster.Fight(7);

        
        Assert.AreEqual(13, monster.CurrentHealth); 
        Assert.AreEqual(25, player.CurrentHealth);   
    }
    [Test]
    public void Fight_MonsterDies_WhenHealthReachesZero()
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
        monster.Fight(20); 
        
        Assert.AreEqual(0, monster.CurrentHealth); 
        Assert.AreEqual(30, player.CurrentHealth);
    }
    [Test]
    public void Fight_NegativeDamage_DoesNotChangeHealth()
    {
       
        var monsterGameObject = new GameObject();
        var monster = monsterGameObject.AddComponent<Dragon>(); 
        monster.MaxHealth = 20;
        monster.CurrentHealth = 20;
        monster.AttackPower = 5;
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
        monster.AttackPower = 5;
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
        monster.AttackPower = 5;
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
        monster.AttackPower = 5;
        player.MaxHealth = 30;
        player.CurrentHealth = 30;
        monster.Player = player;
        monster.Fight(15); 
        
        Assert.AreEqual(0, monster.CurrentHealth); 
        Assert.AreEqual(30, player.CurrentHealth);
    }
}
