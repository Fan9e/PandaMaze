using UnityEngine;

public abstract class Weapon : Item, IWeapon
{
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
        Monster monster = other.GetComponent<Monster>();
        if (monster == null) return;
        if (monster.CurrentHealth == 0) return;

        monster.ReceiveDamage(CalculateDamage());
        Debug.Log(name + " rammer " + monster.name + " for " + CalculateDamage() + " skade");
    }
}    
