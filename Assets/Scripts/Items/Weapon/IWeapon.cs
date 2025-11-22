using UnityEngine;

public interface IWeapon
{
 int CalculateDamage();
 void Attack(Monster monster);
 string AttackAnimationName { get; }


}
