using UnityEngine;

public abstract class Weapon : Item, IWeapon
{
    public abstract int CalculateDamage();
    public bool isAttacking = false;


    //TODO Tilføje attack mod monster 
    /// <summary>
    /// 
    /// </summary>
    /// <param name="other"></param>
    private void OnTriggerStay(Collider other)
    {
        if (!isAttacking) return;

        //Under implementering når monster script er lavet
        //if (==0) return; tjekker om det er fjende er død implemter når man har monster script og fjerne sætning
        //if (monster != null) //Tjekker om monster er null
        //  {
        //     monster.TakeDamage(AttackDamage());
        // }
    }
}    
