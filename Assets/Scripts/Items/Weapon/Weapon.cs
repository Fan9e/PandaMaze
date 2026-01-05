using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public abstract class Weapon : Item, IWeapon
{
    [Header("Backpack UI")]
    [Tooltip("Hvilket ikon i BagpackUI der skal bruges (0 = første, 1 = anden, 2 = tredje). " +
             "Hvis værdien er negativ i editoren, sættes den automatisk ud fra navnet.")]
    [SerializeField] private int backpackVariantIndex = -1;

    /// <summary>
    /// Index til hvilket våben-ikon der skal vises i <see cref="BagpackUI"/>.
    /// Bruges til at vælge elementet i weaponSprites-arrayet (0 = første, 1 = anden, 2 = tredje).
    /// </summary>
    public int BackpackVariantIndex => backpackVariantIndex;

    [Header("Weapon UI")]

    [SerializeField, Tooltip("Navnet på attack-animationen. Tom = bruger klassens navn.")]
    private string _attackAnimationName;

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
        socketTransform.localPosition = Vector3.zero;
        socketTransform.localEulerAngles = Vector3.zero;
        socketTransform.localScale = Vector3.zero;
    }

    /// <summary>
    /// Returnerer navnet på angrebs-animationen.
    /// Bruger <see cref="_attackAnimationName"/> hvis det er sat;
    /// ellers bruges klassens navn
    /// </summary>
    public string AttackAnimationName =>
       string.IsNullOrWhiteSpace(_attackAnimationName)
           ? GetType().Name
           : _attackAnimationName;
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
        monster.Fight(damage, true);
    }

#if UNITY_EDITOR

    /// <summary>
    /// Kaldt af Unity i editoren, når værdier ændres i Inspector eller scriptet recompiles.
    /// Sørger for, at standard attack-animationsnavn og backpack-variant bliver sat automatisk,
    /// hvis felterne endnu ikke er udfyldt.
    /// </summary>
    protected virtual void OnValidate()
    {
        AssignDefaultAttackAnimationNameIfEmpty();
        AutoAssignBackpackVariantFromNameIfUnset();
    }

    /// <summary>
    /// Sætter automatisk _attackAnimationName til klassens navn,
    /// hvis feltet er tomt eller ikke sat.
    /// </summary>
    private void AssignDefaultAttackAnimationNameIfEmpty()
    {
        if (string.IsNullOrWhiteSpace(_attackAnimationName))
            _attackAnimationName = GetType().Name;
    }

    /// <summary>
    /// Sætter automatisk <c>backpackVariantIndex</c> ud fra objektets navn,
    /// hvis feltet endnu ikke er sat (dvs. har en negativ værdi).
    /// </summary>
    /// <remarks>
    /// Hvis navnet indeholder "OneHandSword" bruges index 0.
    /// Hvis navnet indeholder "Axe" bruges index 1.
    /// Hvis navnet indeholder "TwoHandSword" bruges index 2.
    /// Hvis ingen af disse matcher, bruges 0 som standardværdi.
    /// </remarks>
    private void AutoAssignBackpackVariantFromNameIfUnset()
    {
        
        if (backpackVariantIndex >= 0)
            return;

        string weaponObjectName = name; 

        if (weaponObjectName.Contains("OneHandSword"))
            backpackVariantIndex = 0;
        else if (weaponObjectName.Contains("Axe"))
            backpackVariantIndex = 1;
        else if (weaponObjectName.Contains("TwoHandSword"))
            backpackVariantIndex = 2;
        else
            backpackVariantIndex = 0; 
    }
#endif


}
