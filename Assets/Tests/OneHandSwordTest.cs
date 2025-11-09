using System.Collections;
using NUnit.Framework;
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

}

