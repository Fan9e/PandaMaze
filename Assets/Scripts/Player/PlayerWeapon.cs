using System.Collections;
using UnityEngine;

public class PlayerWeapon : MonoBehaviour
{
    [Header("Weapon Auto Setup")]
    [Tooltip("Navnet på det child, som bruges som våben-socket.")]
    [SerializeField]
    private string weaponSocketChildName = "WeaponPivot";

    [Tooltip("Navnet på den Layer, som alle monstre ligger på (for eksempel 'Monster').")]
    [SerializeField]
    private string monsterLayerName = "Monster";

    [Tooltip("Hvis den er tom, finder PlayerWeapon selv det første Weapon i sine children.")]
    [SerializeField]
    private Weapon equippedWeaponComponent;

    [Tooltip("Hvis den er tom, finder PlayerWeapon selv socket-transformen via weaponSocketChildName.")]
    [SerializeField]
    private Transform weaponSocketTransform;

    [Tooltip("Hvis den er tom, finder PlayerWeapon selv en Animator, der kan bruges til våben-animationer.")]
    [SerializeField]
    private Animator weaponAnimator;

    [Header("Weapon Prefabs")]
    [SerializeField]
    private Weapon startingWeaponPrefab;

    /// <summary>
    /// Det aktuelt udstyrede våben, tilgået via IWeapon-interfacet.
    /// </summary>
    public IWeapon EquippedWeaponInterface { get; private set; }

    [Header("Attack Settings")]
    [SerializeField]
    private float attackRadius = 2f;

    [SerializeField]
    private float attackDistanceForwardFromPlayer = 1f;

    [SerializeField]
    private LayerMask monsterLayerMask;

    [Tooltip("Hvis sand, læser PlayerWeapon selv input i Update.")]
    [SerializeField]
    private bool handleInputInThisComponent = true;

    private bool isCurrentlyAttacking;


    #region Unity Lifecycle

    /// <summary>
    /// Initialiserer våbensystemet, når objektet bliver oprettet.
    /// Finder LayerMask til monstre, våben-socket, våben-komponent
    /// og den tilhørende Animator.
    /// </summary>
    private void Awake()
    {
        SetupMonsterLayerMask();
        SetupWeaponSocketTransform();
        SetupWeaponComponent();
        SetupWeaponAnimator();
    }

    /// <summary>
    /// Hvis handleInputInThisComponent er sand, håndteres angrebsinput her.
    /// Ellers kan en anden komponent (for eksempel Player) kalde HandleAttackPlayerInput manuelt.
    /// </summary>
    private void Update()
    {
        if (handleInputInThisComponent)
        {
            HandleAttackPlayerInput();
        }
    }

    #endregion


    #region Setup Methods

    /// <summary>
    /// Sikrer at monsterLayerMask er sat til den korrekte Layer for monstre.
    /// Hvis masken endnu ikke har nogen værdi (0), og monsterLayerName ikke er tom,
    /// oprettes en LayerMask ud fra navnet (for eksempel "Monster").
    /// </summary>
    private void SetupMonsterLayerMask()
    {
        if (monsterLayerMask.value == 0 && !string.IsNullOrEmpty(monsterLayerName))
        {
            monsterLayerMask = LayerMask.GetMask(monsterLayerName);
        }
    }

    /// <summary>
    /// Finder og sætter den transform, som våbnet skal bruge som socket på spilleren.
    /// Hvis weaponSocketTransform allerede er sat i Inspector, gør metoden ikke noget.
    /// Ellers forsøger den først at finde et child med navnet weaponSocketChildName
    /// på Player-objektet. Hvis det findes, bruges det som socket.
    /// Hvis der ikke findes noget matchende child, falder den tilbage til at bruge
    /// spillerens egen Transform som socket.
    /// </summary>
    private void SetupWeaponSocketTransform()
    {
        if (weaponSocketTransform != null)
        {
            return;
        }

        if (!string.IsNullOrEmpty(weaponSocketChildName))
        {
            Transform foundSocketTransform = transform.Find(weaponSocketChildName);
            if (foundSocketTransform != null)
            {
                weaponSocketTransform = foundSocketTransform;
                return;
            }
        }

        weaponSocketTransform = transform;
    }

    /// <summary>
    /// Finder eller opretter det våben, som spilleren skal bruge.
    /// Hvis der allerede sidder et Weapon som child, bruges det.
    /// Ellers bliver startingWeaponPrefab instansieret på weaponSocketTransform.
    /// Til sidst sættes EquippedWeaponInterface, så Player kan kalde Attack på våbnet.
    /// </summary>
    private void SetupWeaponComponent()
    {
        if (equippedWeaponComponent == null)
        {
            equippedWeaponComponent = GetComponentInChildren<Weapon>();
        }

        if (equippedWeaponComponent == null && startingWeaponPrefab != null)
        {
            if (weaponSocketTransform == null)
            {
                Debug.LogError("weaponSocketTransform er ikke sat, kan ikke spawne våbnet.", this);
                return;
            }

            equippedWeaponComponent = Instantiate(startingWeaponPrefab, weaponSocketTransform);
            equippedWeaponComponent.transform.localPosition = Vector3.zero;
            equippedWeaponComponent.transform.localRotation = Quaternion.identity;
        }

        if (equippedWeaponComponent == null)
        {
            Debug.LogError("Player kunne hverken finde eller oprette noget Weapon.", this);
            return;
        }

        if (weaponSocketTransform != null &&
            equippedWeaponComponent.transform.parent != weaponSocketTransform)
        {
            equippedWeaponComponent.transform.SetParent(weaponSocketTransform, true);
        }

        EquippedWeaponInterface = equippedWeaponComponent as IWeapon;
        if (EquippedWeaponInterface == null)
        {
            Debug.LogError("Weapon-komponenten på Player implementerer ikke IWeapon.", equippedWeaponComponent);
        }
    }


    /// <summary>
    /// Finder og sætter den Animator, der skal bruges til våbnets animationer.
    /// Hvis weaponAnimator allerede er sat, gør metoden ikke noget.
    /// Ellers forsøger den i denne rækkefølge:
    /// 1) At finde en Animator på weaponSocketTransform eller dets children.
    /// 2) At finde en Animator på equippedWeaponComponent eller dens parents.
    /// 3) At finde en vilkårlig Animator i spillerens children.
    /// Hvis ingen Animator findes, logges en advarsel.
    /// </summary>
    private void SetupWeaponAnimator()
    {
        if (weaponAnimator != null)
        {
            return;
        }

        if (weaponSocketTransform != null)
        {
            weaponAnimator = weaponSocketTransform.GetComponentInChildren<Animator>();
        }

        if (weaponAnimator == null && equippedWeaponComponent != null)
        {
            weaponAnimator = equippedWeaponComponent.GetComponentInParent<Animator>();
        }

        if (weaponAnimator == null)
        {
            weaponAnimator = GetComponentInChildren<Animator>();
        }

        if (weaponAnimator == null)
        {
            Debug.LogWarning("PlayerWeapon kunne ikke finde nogen Animator til våbnet.", this);
        }
    }

    #endregion


    #region Combat And Attacking

    /// <summary>
    /// Håndterer spillerens input til angreb.
    /// Tjekker om venstre museknap er trykket, om der allerede er et angreb i gang,
    /// om der er et våben udstyret, og om der findes et monster inden for angrebsrækkevidde.
    /// Hvis alle betingelser er opfyldt, startes et angreb mod det nærmeste monster.
    /// </summary>
    public void HandleAttackPlayerInput()
    {
        if (!Input.GetMouseButtonDown(0))
        {
            return;
        }

        if (isCurrentlyAttacking)
        {
            return;
        }

        if (EquippedWeaponInterface == null)
        {
            return;
        }

        Monster monster = GetClosestMonsterInAttackRange();
        if (monster == null)
        {
            return;
        }

        StartCoroutine(AttackRoutineCoroutine(monster));
    }

    /// <summary>
    /// Håndterer et fuldt angreb mod det angivne monster.
    /// Sætter spilleren i angrebstilstand, afspiller angrebsanimationen
    /// og påfører derefter skade via det udstyrede våben,
    /// hvis målet stadig er gyldigt.
    /// </summary>
    /// <param name="monster">Det Monster, som spilleren forsøger at angribe.</param>
    private IEnumerator AttackRoutineCoroutine(Monster monster)
    {
        isCurrentlyAttacking = true;

        yield return PlayAttackAnimationCoroutine();

        if (monster != null && EquippedWeaponInterface != null)
        {
            EquippedWeaponInterface.Attack(monster);
        }

        isCurrentlyAttacking = false;
    }

    /// <summary>
    /// Afspiller våbnets angrebsanimation på <see cref="weaponAnimator"/> 
    /// baseret på navnet fra <see cref="IWeapon.AttackAnimationName"/>.
    /// Metoden venter cirka til animationen er færdig, før coroutine-forløbet fortsætter.
    /// Hvis der ikke findes en gyldig animator eller et udstyret våben,
    /// bliver animationen ikke afspillet, og coroutine’en afsluttes med det samme.
    /// </summary>
    /// <returns>
    /// En <see cref="IEnumerator"/>, som bruges af Unitys coroutine-system
    /// til at afvikle animationens varighed over flere frames.
    /// </returns>
    private IEnumerator PlayAttackAnimationCoroutine()
    {
        if (weaponAnimator == null || EquippedWeaponInterface == null)
            yield break;

        string attackAnimationName = EquippedWeaponInterface.AttackAnimationName;
        Debug.Log($"Prøver at spille animation-state: {attackAnimationName}");

        weaponAnimator.Play(attackAnimationName, 0, 0f);

        yield return null;

        AnimatorStateInfo animatorStateInfo = weaponAnimator.GetCurrentAnimatorStateInfo(0);
        float animationLengthInSeconds = animatorStateInfo.length;

        if (animationLengthInSeconds <= 0.05f)
        {
            animationLengthInSeconds = 0.3f;
        }

        yield return new WaitForSeconds(animationLengthInSeconds);
    }

    /// <summary>
    /// Finder det nærmeste monster, som befinder sig inden for spillerens angrebsområde.
    /// Angrebsområdet er en usynlig kugle foran spilleren med centrum i
    /// spillerens position plus fremad-retningen gange attackDistanceForwardFromPlayer.
    /// Kun colliders på monsterLayerMask bliver taget med.
    /// </summary>
    /// <returns>
    /// Det nærmeste Monster inden for rækkevidde,
    /// eller null hvis der ikke er nogen monstre i angrebsområdet.
    /// </returns>
    private Monster GetClosestMonsterInAttackRange()
    {
        Vector3 attackCenter = transform.position + transform.forward * attackDistanceForwardFromPlayer;

        Collider[] hitColliders = Physics.OverlapSphere(attackCenter, attackRadius, monsterLayerMask);

        Monster closestMonster = null;
        float closestDistanceSquared = Mathf.Infinity;

        foreach (Collider hitCollider in hitColliders)
        {
            Monster monster = hitCollider.GetComponentInParent<Monster>();
            if (monster == null)
            {
                continue;
            }

            float distanceSquared =
                (monster.transform.position - attackCenter).sqrMagnitude;

            if (distanceSquared < closestDistanceSquared)
            {
                closestDistanceSquared = distanceSquared;
                closestMonster = monster;
            }
        }

        return closestMonster;
    }

    #endregion


    #region Weapon switching and equipping

    /// <summary>
    /// Udstyrer spilleren med et nyt våben.
    /// Det gamle våben bliver fjernet (destrueret), og det nye våben
    /// bliver placeret på våben-socket'en og gjort til det aktive våben.
    /// </summary>
    /// <param name="newWeaponComponent">
    /// Den nye Weapon-komponent, som spilleren skal bruge.
    /// </param>
    public void EquipNewWeapon(Weapon weaponPrefab)
    {
        if (weaponPrefab == null)
        {
            Debug.LogWarning("EquipNewWeapon blev kaldt med null.", this);
            return;
        }

        if (equippedWeaponComponent != null)
        {
            Destroy(equippedWeaponComponent.gameObject);
        }

        if (weaponSocketTransform == null)
        {
            Debug.LogError("weaponSocketTransform er ikke sat, kan ikke equipppe nyt våben.", this);
            return;
        }

        equippedWeaponComponent = Instantiate(weaponPrefab, weaponSocketTransform);
        equippedWeaponComponent.transform.localPosition = Vector3.zero;
        equippedWeaponComponent.transform.localRotation = Quaternion.identity;

        EquippedWeaponInterface = equippedWeaponComponent as IWeapon;
        if (EquippedWeaponInterface == null)
        {
            Debug.LogError("Det nye Weapon implementerer ikke IWeapon.", equippedWeaponComponent);
        }

        SetupWeaponAnimator();
    }


    #endregion
}
