using UnityEngine;

public abstract class Weapon : Item, IWeapon
{
    public abstract int Attack();
}
