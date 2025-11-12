using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public abstract class Weapon : Item, IWeapon
{
    Monster monster;
    public abstract int CalculateDamage();
    public bool isAttacking = false;

    /// <summary>
    /// Håndterer, når våbnets trigger er i kontakt med et andet objekt.
    /// Hvis våbnet er i gang med at angribe og objektet er et monster,
    /// påføres monstret skade.
    /// </summary>
    /// <param name="other">Den collider, der befinder sig inde i våbnets trigger.</param>
    private void OnTriggerStay(Collider other)
    {
        if (!isAttacking) return;
        if (monster == null) return;
        DealDamageToMonster(monster);
    }

    /// <summary>
    /// Udfører et slag mod det angivne monster ved at beregne skaden
    /// og anvende den.
    /// </summary>
    /// <param name="monster">Det monster, der skal modtage skaden</param>
    private void DealDamageToMonster(Monster monster)
    {
        int dmg = CalculateDamage();
        monster.Fight(dmg);
    }
}    
