using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public abstract class Weapon : Item, IWeapon
{
    [SerializeField] private string _attackAnimOverride;
    /// <summary>
    /// Returnerer navnet på angrebs-animationen.
    /// Bruger <see cref="_attackAnimOverride"/> hvis det er sat;
    /// ellers bruges klassens navn efterfulgt af "Attack"
    /// </summary>
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
    public void Attack(Monster monster)
    {
        if (!isAttacking) return;
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
    /// <summary>
    /// Kører kun i Unity Editor og sørger for,
    /// at <see cref="_attackAnimOverride"/> får en fornuftig standardværdi.
    /// Hvis feltet er tomt i Inspector, sættes det automatisk til navnet
    /// på den type/komponent, som scriptet sidder på.
    /// </summary>
    protected virtual void OnValidate()
    {
        if (string.IsNullOrEmpty(_attackAnimOverride))
            _attackAnimOverride = GetType().Name;
    }
#endif
}
