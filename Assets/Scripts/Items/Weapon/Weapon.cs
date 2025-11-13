using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public abstract class Weapon : Item, IWeapon
{
    public abstract int CalculateDamage();
    public bool isAttacking = false;

    /// <summary>
    /// Kaldes når spilleren har sagt ordet rigtigt
    /// og vi vil slå et bestemt monster.
    /// </summary>
    /// <param name="monster">Det monster, der skal modtage skaden</param>
    public void Attack(Monster monster)
    {
        if (monster == null) return;
        DealDamage(monster);
    }

    /// <summary>
    /// Udfører et slag mod det angivne monster ved at beregne skaden
    /// og anvende den.
    /// </summary>
    /// <param name="monster">Det monster, der skal modtage skaden</param>
    private void DealDamage(Monster monster)
    {
        int dmg = CalculateDamage();
        monster.Fight(dmg);
    }
}    
