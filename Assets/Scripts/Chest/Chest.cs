using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public abstract class Chest : MonoBehaviour
{
    [Header("Kiste-indstillinger")]

    [Tooltip("True når kisten allerede er åbnet (kun én gang).")]
    [SerializeField] private bool isOpened;

    [Tooltip("Dot-product tærskel for at spilleren betragtes som værende foran kisten.")]
    [SerializeField] private float frontDotThreshold = 0.5f;

    [Tooltip("Maksimal afstand til spilleren før kisten kan åbnes.")]
    public float openDistance = 1.2f; 
    
    [Header("Animation")]
    [Tooltip("Animator-controlleren der bruges, når kisten åbnes.")]
    public RuntimeAnimatorController cachedController;

    [SerializeField]
    [Tooltip("Ekstra ventetid efter åbne-animationen før loot gives til spilleren.")]
    private float lootDelayAfterOpen = 0.4f;

    [SerializeField]
    [Tooltip("Navn på trigger-parameteren i Animator til at starte åbne-animationen. Tom streng = ingen trigger.")]
    private string openTriggerName = "Open";

    [Header("UI")]
    [Tooltip("Fælles UIMessageManager til alle kister. Hvis ikke sat i Inspector, forsøges den fundet automatisk.")]
    [SerializeField] protected UIMessageManager uiMessageManager;
    [SerializeField] protected Transform player;
    [SerializeField] protected PlayerInventory playerInventory;
    [SerializeField] protected PlayerWeapon playerWeapon;
    [SerializeField] protected Animator animator;

    /// <summary>
    /// Initialiserer de nødvendige referencer til animator, spiller og UI.
    /// </summary>
    private void Awake()
    {
        InitializeAnimator();
        InitializePlayer();
        InitializeUIMessageManager();
    }
    /// <summary>
    /// Tjekker hvert frame om kisten kan åbnes.
    /// </summary>
    private void Update()
    {
        TryOpenChest();
    }

    /// <summary>
    /// Ekstra betingelser for at åbne kisten (overstyr i subklasser).
    /// Eksempel: "bossen skal være død", "spilleren skal have en nøgle", osv.
    /// </summary>
    protected virtual bool CanOpen() => true;

    /// <summary>
    /// Opretter kistens loot (implementeres i subklasser).
    /// </summary>
    /// <returns>Et <see cref="IChestLoot"/>-objekt, eller null hvis der ikke skal gives loot.</returns>
    protected abstract IChestLoot CreateLoot();

    /// <summary>
    /// Finder og sætter <see cref="uiMessageManager"/>, hvis den ikke allerede er sat i Inspector.
    /// Prøver først via singleton (<see cref="UIMessageManager.Instance"/>), derefter via søgning i scenen.
    /// </summary>
    protected void InitializeUIMessageManager()
    {
        if (uiMessageManager != null) return;

        uiMessageManager = UIMessageManager.Instance;
        if (uiMessageManager == null)
            uiMessageManager = FindObjectOfType<UIMessageManager>();

        if (uiMessageManager == null)
            Debug.LogWarning($"{nameof(Chest)}: kunne ikke finde {nameof(UIMessageManager)} i scenen.", this);
    }

    /// <summary>
    /// Finder animatoren i børnene og cacher dens controller.
    /// Controlleren sættes til null indtil kisten åbnes.
    /// </summary>
    private void InitializeAnimator()
    {
        animator = GetComponentInChildren<Animator>();

        if (animator == null)
        {
            Debug.LogError("Chest: Fandt ingen Animator!", this);
            return;
        }

        cachedController = animator.runtimeAnimatorController;
        animator.runtimeAnimatorController = null; 
    }

    /// <summary>
    /// Finder spilleren via tagget "Player" og cacher relevante komponenter.
    /// </summary>
    private void InitializePlayer()
    {
        GameObject playerObject = GameObject.FindWithTag("Player");

        if (playerObject == null)
        {
            Debug.LogError($"{nameof(Chest)}: Ingen GameObject med tag 'Player' fundet!", this);
            return;
        }

        player = playerObject.transform;
        playerInventory = playerObject.GetComponent<PlayerInventory>();
        playerWeapon = playerObject.GetComponent<PlayerWeapon>();
    }

    /// <summary>
    /// Forsøger at åbne kisten, hvis spilleren er tæt nok på, står foran kisten
    /// og ekstra betingelser er opfyldt.
    /// </summary>
    private void TryOpenChest()
    {
        if (isOpened || player == null || animator == null)
            return;

        if (!IsPlayerCloseEnough(out Vector3 toPlayer))
            return;

        if (!IsPlayerInFront(toPlayer))
            return;

        if (!CanOpen())
            return;

        StartOpenChest();
    }

    /// <summary>
    /// Tjekker om spilleren er indenfor åbnings-afstand.
    /// </summary>
    private bool IsPlayerCloseEnough(out Vector3 toPlayer)
    {
        toPlayer = player.position - transform.position;
        float maxDistSqr = openDistance * openDistance;
        return toPlayer.sqrMagnitude <= maxDistSqr;
    }
    /// <summary>
    /// Tjekker om spilleren står foran kisten (dot product).
    /// </summary>
    private bool IsPlayerInFront(Vector3 toPlayer)
    {
        Vector3 dirToPlayer = toPlayer.normalized;
        float dot = Vector3.Dot(transform.forward, dirToPlayer);
        return dot > frontDotThreshold;
    }

    /// <summary>
    /// Markerer kisten som åbnet og starter coroutine til animation + loot.
    /// </summary>
    private void StartOpenChest()
    {
        if (isOpened)
            return;

        isOpened = true;
        StartCoroutine(OpenChestRoutine());
    }

    /// <summary>
    /// Afspiller åbne-animationen og giver loot efter animationens længde + ekstra delay.
    /// </summary>
    private IEnumerator OpenChestRoutine()
    {
        if (cachedController != null)
            animator.runtimeAnimatorController = cachedController;

        if (!string.IsNullOrEmpty(openTriggerName))
            animator.SetTrigger(openTriggerName);

        yield return null;

        animator.Update(0f);

        float animLength = animator.GetCurrentAnimatorStateInfo(0).length;
        float waitTime = Mathf.Max(0f, animLength) + lootDelayAfterOpen;

        if (waitTime > 0f)
            yield return new WaitForSeconds(waitTime);

        IChestLoot loot = CreateLoot();
        loot?.GiveItemsToPlayer(playerInventory, playerWeapon);
    }
}


