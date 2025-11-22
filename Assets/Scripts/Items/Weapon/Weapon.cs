using UnityEngine;

public abstract class Weapon : Item, IWeapon
{
    [SerializeField] private string _attackAnimOverride;
    public string AttackAnimationName =>
       string.IsNullOrEmpty(_attackAnimOverride)
           ? GetType().Name + "Attack"
           : _attackAnimOverride;
    public abstract int CalculateDamage();

    /// <summary>
    /// Kaldes når spilleren har sagt ordet rigtigt
    /// og vi vil slå et bestemt monster.
    /// </summary>
    /// <param name="monster">Det monster, der skal modtage skaden</param>
    public virtual void Attack(Monster monster)
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
        int damage = CalculateDamage();
        monster.Fight(damage);
    }

#if UNITY_EDITOR
    protected virtual void OnValidate()
    {
        if (string.IsNullOrEmpty(_attackAnimOverride))
            _attackAnimOverride = GetType().Name;  
    }
#endif
}
