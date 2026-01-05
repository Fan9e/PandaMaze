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
    private LayerMask monsterLayerMask;

    [Tooltip("Hvis sand, læser PlayerWeapon selv input i Update.")]
    [SerializeField]
    private bool handleInputInThisComponent = true;
    /// <summary>
    /// Sand mens en angrebs-coroutine kører, så vi undgår overlappende angreb.
    /// </summary>
    private bool isCurrentlyAttacking;

    /// <summary>
    /// Reference til den angrebs-coroutine der kører, hvis der er en i gang.
    /// </summary>
    private Coroutine attackRoutine;

    /// <summary>
    /// Reserve-ventetid hvis animatoren rapporterer meget kort eller ingen længde.
    /// </summary>
    private const float FallbackAnimLength = 0.3f;

    #region Unity Lifecycle

    /// <summary>
    /// Unity-callback. Kører ved oprettelse og sikrer at lagmaske, våbenfatning,
    /// våben og animator er sat korrekt.
    /// </summary>
    private void Awake()
    { 
        if (bagpackUI == null)
        {
            bagpackUI = FindObjectOfType<BagpackUI>();
        }
        EnsureMonsterLayerMask();
        EnsureWeaponSocket();
        EnsureWeaponEquipped();
        EnsureWeaponAnimator();
    }

    #endregion


    #region Setup Methods

    /// <summary>
    /// Sikrer at monsterLayerMask er sat til den korrekte Layer for monstre.
    /// Hvis masken endnu ikke har nogen værdi (0), og monsterLayerName ikke er tom,
    /// oprettes en LayerMask ud fra navnet (for eksempel "Monster").
    /// </summary>
    private void EnsureMonsterLayerMask()
    {
        if (monsterLayerMask.value == 0 && !string.IsNullOrEmpty(monsterLayerName))
        {
            monsterLayerMask = LayerMask.GetMask(monsterLayerName);
        }
    }

    /// <summary>
    /// Sikrer at våbenfatningens transform er sat.
    /// Finder et child-objekt med det angivne navn, hvis muligt,
    /// ellers bruges spillerens egen transform som reserve.
    /// </summary>
    private void EnsureWeaponSocket()
    {
        if (weaponSocketTransform != null) return;

        if (!string.IsNullOrEmpty(weaponSocketChildName))
        {
            var foundSocketTransform = transform.Find(weaponSocketChildName);
            if (foundSocketTransform != null)
            {
                weaponSocketTransform = foundSocketTransform;
                return;
            }
        }

        weaponSocketTransform = transform;
    }

    /// <summary>
    /// Sikrer at spilleren har et våben:
    /// Finder et eksisterende våben i child-objekter, hvis muligt.
    /// Ellers instansieres startvåbenet, hvis det er sat.
    /// Sikrer derefter at våbnet sidder på våbenfatningen og sætter interfacet.
    /// </summary>
    private void EnsureWeaponEquipped()
    {
        if (equippedWeaponComponent == null)
            equippedWeaponComponent = GetComponentInChildren<Weapon>();

        if (equippedWeaponComponent == null && startingWeaponPrefab != null)
            EquipWeaponInternal(startingWeaponPrefab, destroyOld: false);

        if (equippedWeaponComponent == null)
        {
            Debug.LogError("Spilleren kunne hverken finde eller oprette et våben.", this);
            EquippedIWeapon = null;
            return;
        }

        if (weaponSocketTransform != null && equippedWeaponComponent.transform.parent != weaponSocketTransform)
            equippedWeaponComponent.transform.SetParent(weaponSocketTransform, worldPositionStays: true);

        EquippedIWeapon = equippedWeaponComponent as IWeapon;
        if (EquippedIWeapon == null)
            Debug.LogError("Våben-komponenten på spilleren implementerer ikke IWeapon-interfacet.", equippedWeaponComponent);

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
    /// Sikrer at animatoren til våbenanimationer er sat.
    /// Forsøger i denne rækkefølge:
    /// Animator på våbenfatningen eller dens child-objekter.
    /// Animator på våbnet eller dets forældre-objekter.
    /// Animator et vilkårligt sted i spillerens child-objekter.
    /// Hvis ingen animator findes, logges en advarsel.
    /// </summary>
    private void EnsureWeaponAnimator()
    {
        if (weaponAnimator != null) return;

        if (weaponSocketTransform != null)
            weaponAnimator = weaponSocketTransform.GetComponentInChildren<Animator>();

        if (weaponAnimator == null && equippedWeaponComponent != null)
            weaponAnimator = equippedWeaponComponent.GetComponentInParent<Animator>();

        if (weaponAnimator == null)
            weaponAnimator = GetComponentInChildren<Animator>();

        if (weaponAnimator == null)
            Debug.LogWarning("PlayerWeapon kunne ikke finde en animator til våbenanimationer.", this);
    }
    #endregion


    #region Combat And Attacking

    /// <summary>
    /// Udfører et angreb mod et specifikt monster, hvis spilleren ikke allerede angriber,
    /// der er et våben udstyret, og monster-referencen er gyldig.
    /// Angrebet håndteres i en coroutine, så vi kan afspille animation og vente korrekt.
    /// </summary>
    /// <param name="monster">Monsteret der skal angribes.</param>
    public void AttackSpecificMonster(Monster monster)
    {
        if (monster == null) return;
        if (EquippedIWeapon == null) return;
        if (isCurrentlyAttacking) return;

        if (attackRoutine != null)
            StopCoroutine(attackRoutine);

        attackRoutine = StartCoroutine(HandleAttackSequenceCoroutine(monster));
    }

    /// <summary>
    /// Afvikler et angreb som coroutine:
    /// Sætter at spilleren angriber.
    /// Afspiller våbenets angrebsanimation (hvis muligt) og beregner ventetid.
    /// Kalder våbnets angrebslogik på monsteret.
    /// Venter til animationen forventes at være færdig.
    /// </summary>
    /// <param name="monster">Monsteret der angribes.</param>
    /// <returns>En coroutine der kører hele angrebsforløbet.</returns>
    private IEnumerator HandleAttackSequenceCoroutine(Monster monster)
    {
        isCurrentlyAttacking = true;

        var weapon = EquippedIWeapon;
        if (weapon == null || monster == null)
        {
            isCurrentlyAttacking = false;
            attackRoutine = null;
            yield break;
        }

        float waitTimeInSeconds = 0f;

        if (weaponAnimator != null)
        {
            string attackAnimationName = EquippedIWeapon.AttackAnimationName;

            if (!string.IsNullOrEmpty(attackAnimationName))
            {
                weaponAnimator.Play(attackAnimationName, 0, 0f);
                yield return null;

                AnimatorStateInfo stateInfo = weaponAnimator.GetCurrentAnimatorStateInfo(0);
                waitTimeInSeconds = stateInfo.length;

                if (waitTimeInSeconds <= 0.05f)
                    waitTimeInSeconds = FallbackAnimLength;
            }
        }

        EquippedIWeapon.Attack(monster);

        if (waitTimeInSeconds > 0f)
            yield return new WaitForSeconds(waitTimeInSeconds);

        isCurrentlyAttacking = false;
        attackRoutine = null;
    }

    #endregion


    #region Weapon switching and equipping

    /// <summary>
    /// Udstyrer spilleren med et nyt våben.
    /// Det gamle våben bliver fjernet (destrueret), og det nye våben
    /// bliver instansieret på våbenfatningen og gjort til det aktive våben.
    /// </summary>
    /// <param name="weaponPrefab">Våbenobjektet der skal instansieres og udstyres.</param>
    public void EquipNewWeapon(Weapon weaponPrefab)
    {
        if (weaponPrefab == null)
        {
            Debug.LogWarning("EquipNewWeapon blev kaldt med en null-reference.", this);
            return;
        }

        EquipWeaponInternal(weaponPrefab, destroyOld: true);
        EnsureWeaponAnimator();
    }

    /// <summary>
    /// Intern hjælpemetode der instansierer og udstyrer et våben på våbenfatningen.
    /// Bruges både ved start (startvåben) og ved udskiftning af våben.
    /// </summary>
    /// <param name="weaponPrefab">Våbenobjektet der skal instansieres.</param>
    /// <param name="destroyOld">Hvis sand destrueres det eksisterende våben først.</param>
    private void EquipWeaponInternal(Weapon weaponPrefab, bool destroyOld)
    {
        EnsureWeaponSocket();

        if (weaponSocketTransform == null)
        {
            Debug.LogError("Våbenfatningens transform er ikke sat, kan ikke instansiere eller udstyre våben.", this);
            return;
        }

        if (destroyOld && equippedWeaponComponent != null)
        {
            if (Application.isPlaying)
                Destroy(equippedWeaponComponent.gameObject);

            else
                Destroy(equippedWeaponComponent.gameObject);

        }

        equippedWeaponComponent = Instantiate(weaponPrefab, weaponSocketTransform);
        equippedWeaponComponent.ConfigureSocketTransform(equippedWeaponComponent.transform);

        EquippedIWeapon = equippedWeaponComponent as IWeapon;
        if (EquippedIWeapon == null)
            Debug.LogError("Det nye våben implementerer ikke IWeapon-interfacet.", equippedWeaponComponent);
        UpdateWeaponUI();
    }

    #endregion
}
