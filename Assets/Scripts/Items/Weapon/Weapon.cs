using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public abstract class Weapon : Item, IWeapon
{
    [SerializeField] private string _attackAnimOverride;

    [Header("Socket Offset")]
    /// <summary>
    /// Lokal position i forhold til weapon-socket (WeaponPivot).
    /// </summary>
    public Vector3 socketLocalPosition = Vector3.zero;

    /// <summary>
    /// Lokal rotation (Euler-angles) i forhold til weapon-socket.
    /// </summary>
    public Quaternion socketLocalEulerAngles = Quaternion.identity;

    /// <summary>
    /// Lokal skalering i forhold til weapon-socket.
    /// </summary>
    public Vector3 socketLocalScale = Vector3.one;
    /// <summary>
    /// Konfigurerer transformen for det socket, som våbnet skal sidde i.
    /// Base-implementationen bruger <see cref="socketLocalPosition"/>,
    /// <see cref="socketLocalEulerAngles"/> og <see cref="socketLocalScale"/>.
    /// Konkrete våben (f.eks. Axe, Sword) kan override denne metode,
    /// hvis de har brug for en helt speciel opsætning.
    /// </summary>
    /// <param name="socketTransform">
    /// Transformen for det socket-objekt (fx WeaponPivot på spilleren),
    /// som våbnet skal placeres i.
    /// </param>
    public virtual void ConfigureSocketTransform(Transform socketTransform)
    {
        socketTransform.localPosition = socketLocalPosition;
        socketTransform.localRotation = socketLocalEulerAngles;
        socketTransform.localScale = socketLocalScale;
    }
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
