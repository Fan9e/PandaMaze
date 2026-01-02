using System.Collections;
using UnityEngine;

public class PlayerWeapon : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private BagpackUI bagpackUI;

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
    public IWeapon EquippedIWeapon { get; private set; }

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
        if (bagpackUI == null)
        {
            bagpackUI = FindObjectOfType<BagpackUI>();
        }
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
        if (monsterLayerMask.value != 0 || string.IsNullOrEmpty(monsterLayerName))
            return;

        monsterLayerMask = LayerMask.GetMask(monsterLayerName);

        if (monsterLayerMask.value == 0)
            Debug.LogWarning($"Layer '{monsterLayerName}' findes ikke, eller ingen objekter bruger den. Attack rammer intet.", this);
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

        EquippedIWeapon = equippedWeaponComponent as IWeapon;
        if (EquippedIWeapon == null)
        {
            Debug.LogError("Weapon-komponenten på Player implementerer ikke IWeapon.", equippedWeaponComponent);
        }
        UpdateWeaponUI();
    }
    private void UpdateWeaponUI()
    {
        if (bagpackUI == null)
        {
            Debug.LogWarning("UpdateWeaponUI kaldt, men bagpackUI er null.", this);
            return;
        }

        if (equippedWeaponComponent == null || EquippedIWeapon == null)
        {
            bagpackUI.SetHasWeapon(false);
            Debug.Log("UpdateWeaponUI: intet våben – slår weapon-slot fra");
            return;
        }

        int variant = Mathf.Clamp(equippedWeaponComponent.BackpackVariantIndex, 0, 2);

        bagpackUI.SetHasWeapon(true);
        bagpackUI.SetWeaponVariant(variant);

        Debug.Log($"UpdateWeaponUI: satte weapon-slot til variant {variant} for {equippedWeaponComponent.name}");
    }




    /// <summary>
    /// Finder og sætter den Animator, der skal bruges til våbnets animationer.
    /// Hvis weaponAnimator allerede er sat, gør metoden ikke noget.
    /// Ellers forsøger den i denne rækkefølge:
    /// At finde en Animator på weaponSocketTransform eller dets children.
    /// At finde en Animator på equippedWeaponComponent eller dens parents.
    /// At finde en vilkårlig Animator i spillerens children.
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

    //TODO: ændre at man kan angribe med ordene, i stedet for med musen
    /// <summary>
    /// Håndterer spillerens input til angreb.
    /// Tjekker om venstre museknap er trykket, om der allerede er et angreb i gang,
    /// om der er et våben udstyret, og om der findes et monster inden for angrebsrækkevidde.
    /// Hvis alle betingelser er opfyldt, startes et angreb mod det nærmeste monster.
    /// </summary>
    public void HandleAttackPlayerInput()
    {
        if (!Input.GetMouseButtonDown(0)) return;
        Debug.Log("Klik registreret");

        if (isCurrentlyAttacking) { Debug.Log("Stop: isCurrentlyAttacking"); return; }
        if (EquippedIWeapon == null) { Debug.Log("Stop: EquippedIWeapon == null"); return; }

        Monster monster = GetClosestMonsterInAttackRange();
        if (monster == null) { Debug.Log("Stop: ingen monster i range"); return; }

        Debug.Log("Starter attack coroutine");
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

        if (monster != null && EquippedIWeapon != null)
        {
            EquippedIWeapon.Attack(monster);
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
        if (weaponAnimator == null || EquippedIWeapon == null)
            yield break;

        string attackAnimationName = EquippedIWeapon.AttackAnimationName;
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
    /// <param name="weaponPrefab">Våben-prefab der skal equips.</param>
    public void EquipNewWeapon(Weapon weaponPrefab)
    {
        if (weaponPrefab == null)
        {
            Debug.LogWarning("EquipNewWeapon blev kaldt med null.", this);
            return;
        }

        if (weaponSocketTransform == null)
        {
            Debug.LogError("weaponSocketTransform er ikke sat, kan ikke equipppe nyt våben.", this);
            return;
        }

        Debug.Log($"EquipNewWeapon: prøver at equippe {weaponPrefab} på socket {weaponSocketTransform.name}");

        if (equippedWeaponComponent != null)
        {
            Debug.Log("EquipNewWeapon: Destroyer gammelt våben: " + equippedWeaponComponent.name);
            Destroy(equippedWeaponComponent.gameObject);
        }
        
 
        equippedWeaponComponent = Instantiate(weaponPrefab, weaponSocketTransform);

        equippedWeaponComponent.ConfigureSocketTransform(equippedWeaponComponent.transform);

        Debug.Log("EquipNewWeapon: Nyt våben-instans: " + equippedWeaponComponent.name);
        EquippedIWeapon = equippedWeaponComponent as IWeapon;
        if (EquippedIWeapon == null)
        {
            Debug.LogError("Det nye Weapon implementerer ikke IWeapon.", equippedWeaponComponent);
        }

        SetupWeaponAnimator();

        for (int i = 0; i < weaponSocketTransform.childCount; i++)
        {
            Transform child = weaponSocketTransform.GetChild(i);
            Debug.Log($"EquipNewWeapon: Socket-child {i}: {child.name}", child);
        }
        UpdateWeaponUI();
    }

    

    #endregion
}
