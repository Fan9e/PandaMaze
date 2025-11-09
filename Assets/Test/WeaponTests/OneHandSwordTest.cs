using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class OneHandSwordTest
{
    [Test]
    public void OneHandSword_Attack_Returns10()
    {
        // Arrange: lav et tomt GameObject og tilføj sværdet
        var go = new GameObject();
        var sword = go.AddComponent<OneHandSword>();

        // Act
        int damage = sword.Attack();

        // Assert
        Assert.AreEqual(10, damage);
    }

  
}

