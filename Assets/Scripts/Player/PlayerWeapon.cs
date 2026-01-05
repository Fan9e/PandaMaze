using System.Collections;
using UnityEngine;

public class PlayerWeapon : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private BagpackUI bagpackUI;

    [Header("Weapon Auto Setup")]
    [Tooltip("Navnet p� det child, som bruges som v�ben-socket.")]
    [SerializeField]
    private string weaponSocketChildName = "WeaponPivot";

    [Tooltip("Navnet p� den Layer, som alle monstre ligger p� (for eksempel 'Monster').")]
    [SerializeField]
    private string monsterLayerName = "Monster";

    [Tooltip("Hvis den er tom, finder PlayerWeapon selv det f�rste Weapon i sine children.")]
    [SerializeField]
    private Weapon equippedWeaponComponent;

    [Tooltip("Hvis den er tom, finder PlayerWeapon selv socket-transformen via weaponSocketChildName.")]
    [SerializeField]
    private Transform weaponSocketTransform;

    [Tooltip("Hvis den er tom, finder PlayerWeapon selv en Animator, der kan bruges til v�ben-animationer.")]
    [SerializeField]
    private Animator weaponAnimator;

    [Header("Weapon Prefabs")]
    [SerializeField]
    private Weapon startingWeaponPrefab;

    /// <summary>
    /// Det aktuelt udstyrede v�ben, tilg�et via IWeapon-interfacet.
    /// </summary>
    public IWeapon EquippedIWeapon { get; private set; }

    [Header("Attack Settings")]
    [SerializeField]
    private LayerMask monsterLayerMask;

    [Tooltip("Hvis sand, l�ser PlayerWeapon selv input i Update.")]
    [SerializeField]
    private bool handleInputInThisComponent = true;
    /// <summary>
    /// Sand mens en angrebs-coroutine k�rer, s� vi undg�r overlappende angreb.
    /// </summary>
    private bool isCurrentlyAttacking;

    /// <summary>
    /// Reference til den angrebs-coroutine der k�rer, hvis der er en i gang.
    /// </summary>
    private Coroutine attackRoutine;

    /// <summary>
    /// Reserve-ventetid hvis animatoren rapporterer meget kort eller ingen l�ngde.
    /// </summary>
    private const float FallbackAnimLength = 0.3f;

    #region Unity Lifecycle

    /// <summary>
    /// Unity-callback. K�rer ved oprettelse og sikrer at lagmaske, v�benfatning,
    /// v�ben og animator er sat korrekt.
    /// </summary>
    private void Awake()
    {
        EnsureMonsterLayerMask();
        EnsureWeaponSocket();
        EnsureWeaponEquipped();
        EnsureWeaponAnimator();
    }

    #endregion


    #region Setup Methods

    /// <summary>
    /// Sikrer at monsterLayerMask er sat til den korrekte Layer for monstre.
    /// Hvis masken endnu ikke har nogen v�rdi (0), og monsterLayerName ikke er tom,
    /// oprettes en LayerMask ud fra navnet (for eksempel "Monster").
    /// </summary>
    private void EnsureMonsterLayerMask()
    {
        if (monsterLayerMask.value != 0 || string.IsNullOrEmpty(monsterLayerName))
            return;

        monsterLayerMask = LayerMask.GetMask(monsterLayerName);

        if (monsterLayerMask.value == 0)
            Debug.LogWarning($"Layer '{monsterLayerName}' findes ikke, eller ingen objekter bruger den. Attack rammer intet.", this);
    }

    /// <summary>
    /// Sikrer at v�benfatningens transform er sat.
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
    /// Sikrer at spilleren har et v�ben:
    /// Finder et eksisterende v�ben i child-objekter, hvis muligt.
    /// Ellers instansieres startv�benet, hvis det er sat.
    /// Sikrer derefter at v�bnet sidder p� v�benfatningen og s�tter interfacet.
    /// </summary>
    private void EnsureWeaponEquipped()
    {
        if (equippedWeaponComponent == null)
            equippedWeaponComponent = GetComponentInChildren<Weapon>();

        if (equippedWeaponComponent == null && startingWeaponPrefab != null)
            EquipWeaponInternal(startingWeaponPrefab, destroyOld: false);

        if (equippedWeaponComponent == null)
        {
            Debug.LogError("Spilleren kunne hverken finde eller oprette et v�ben.", this);
            EquippedWeaponInterface = null;
            return;
        }

        if (weaponSocketTransform != null && equippedWeaponComponent.transform.parent != weaponSocketTransform)
            equippedWeaponComponent.transform.SetParent(weaponSocketTransform, worldPositionStays: true);

        EquippedWeaponInterface = equippedWeaponComponent as IWeapon;
        if (EquippedWeaponInterface == null)
            Debug.LogError("V�ben-komponenten p� spilleren implementerer ikke IWeapon-interfacet.", equippedWeaponComponent);
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
            Debug.Log("UpdateWeaponUI: intet v�ben � sl�r weapon-slot fra");
            return;
        }

        int variant = Mathf.Clamp(equippedWeaponComponent.BackpackVariantIndex, 0, 2);

        bagpackUI.SetHasWeapon(true);
        bagpackUI.SetWeaponVariant(variant);

        Debug.Log($"UpdateWeaponUI: satte weapon-slot til variant {variant} for {equippedWeaponComponent.name}");
    }



    /// <summary>
    /// Sikrer at animatoren til v�benanimationer er sat.
    /// Fors�ger i denne r�kkef�lge:
    /// Animator p� v�benfatningen eller dens child-objekter.
    /// Animator p� v�bnet eller dets for�ldre-objekter.
    /// Animator et vilk�rligt sted i spillerens child-objekter.
    /// Hvis ingen animator findes, logges en advarsel.
    /// </summary>
    private void EnsureWeaponAnimator()
    {
        if (weaponAnimator != null) return;

        // 1) V�benfatningens child-objekter
        if (weaponSocketTransform != null)
            weaponAnimator = weaponSocketTransform.GetComponentInChildren<Animator>();

        // 2) V�bnets for�ldre-objekter
        if (weaponAnimator == null && equippedWeaponComponent != null)
            weaponAnimator = equippedWeaponComponent.GetComponentInParent<Animator>();

        // 3) Et vilk�rligt sted i spillerens child-objekter
        if (weaponAnimator == null)
            weaponAnimator = GetComponentInChildren<Animator>();

        if (weaponAnimator == null)
            Debug.LogWarning("PlayerWeapon kunne ikke finde en animator til v�benanimationer.", this);
    }
    #endregion


    #region Combat And Attacking

    /// <summary>
    /// Udf�rer et angreb mod et specifikt monster, hvis spilleren ikke allerede angriber,
    /// der er et v�ben udstyret, og monster-referencen er gyldig.
    /// Angrebet h�ndteres i en coroutine, s� vi kan afspille animation og vente korrekt.
    /// </summary>
    /// <param name="monster">Monsteret der skal angribes.</param>
    public void AttackSpecificMonster(Monster monster)
    {
        if (monster == null) return;
        if (EquippedWeaponInterface == null) return;
        if (isCurrentlyAttacking) return;

        if (attackRoutine != null)
            StopCoroutine(attackRoutine);

        attackRoutine = StartCoroutine(HandleAttackSequenceCoroutine(monster));
    }

    /// <summary>
    /// Afvikler et angreb som coroutine:
    /// S�tter at spilleren angriber.
    /// Afspiller v�benets angrebsanimation (hvis muligt) og beregner ventetid.
    /// Kalder v�bnets angrebslogik p� monsteret.
    /// Venter til animationen forventes at v�re f�rdig.
    /// </summary>
    /// <param name="monster">Monsteret der angribes.</param>
    /// <returns>En coroutine der k�rer hele angrebsforl�bet.</returns>
    private IEnumerator HandleAttackSequenceCoroutine(Monster monster)
    {
        isCurrentlyAttacking = true;

        var weapon = EquippedWeaponInterface;
        if (weapon == null || monster == null)
        {
            isCurrentlyAttacking = false;
            attackRoutine = null;
            yield break;
        }

        float waitTimeInSeconds = 0f;

        if (weaponAnimator != null)
        {
            string attackAnimationName = EquippedWeaponInterface.AttackAnimationName;

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

        EquippedWeaponInterface.Attack(monster);

        if (waitTimeInSeconds > 0f)
            yield return new WaitForSeconds(waitTimeInSeconds);

        isCurrentlyAttacking = false;
        attackRoutine = null;
    }

    #endregion


    #region Weapon switching and equipping

    /// <summary>
    /// Udstyrer spilleren med et nyt v�ben.
    /// Det gamle v�ben bliver fjernet (destrueret), og det nye v�ben
    /// bliver instansieret p� v�benfatningen og gjort til det aktive v�ben.
    /// </summary>
    /// <param name="weaponPrefab">V�benobjektet der skal instansieres og udstyres.</param>
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
    /// Intern hj�lpemetode der instansierer og udstyrer et v�ben p� v�benfatningen.
    /// Bruges b�de ved start (startv�ben) og ved udskiftning af v�ben.
    /// </summary>
    /// <param name="weaponPrefab">V�benobjektet der skal instansieres.</param>
    /// <param name="destroyOld">Hvis sand destrueres det eksisterende v�ben f�rst.</param>
    private void EquipWeaponInternal(Weapon weaponPrefab, bool destroyOld)
    {
        EnsureWeaponSocket();

        if (weaponSocketTransform == null)
        {
            Debug.LogError("V�benfatningens transform er ikke sat, kan ikke instansiere eller udstyre v�ben.", this);
            return;
        }

        if (destroyOld && equippedWeaponComponent != null)
        {
            if (Application.isPlaying)
                Destroy(gameObject);
            else
                DestroyImmediate(gameObject);
        }

        equippedWeaponComponent = Instantiate(weaponPrefab, weaponSocketTransform);
        equippedWeaponComponent.transform.localPosition = Vector3.zero;
        equippedWeaponComponent.transform.localRotation = Quaternion.identity;

        EquippedWeaponInterface = equippedWeaponComponent as IWeapon;
        if (EquippedWeaponInterface == null)
            Debug.LogError("Det nye v�ben implementerer ikke IWeapon-interfacet.", equippedWeaponComponent);
    }

    #endregion
}
