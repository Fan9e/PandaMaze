using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public abstract class Chest : MonoBehaviour
{

    [Header("Chest settings")]
    public bool isOpened = false;
    public float openDistance = 1.2f;
    public float frontDotThreshold = 0.5f;

    [Header("Animation")]
    public RuntimeAnimatorController cachedController;

    // hvor længe vi venter, efter vi har startet åbne-animationen,
    // før vi giver loot til spilleren
    [SerializeField] private float lootDelayAfterOpen = 0.4f;

    // hvis du har en trigger i animatoren (kan være tom)
    [SerializeField] private string openTriggerName = "Open";

    protected Transform player;
    protected PlayerInventory playerInventory;
    protected PlayerWeapon playerWeapon;

    [Header("UI")]
    [Tooltip("Fælles UIMessageManager til alle kister.")]
    [SerializeField] protected UIMessageManager uiMessageManager;


    private Animator animator;

    private void Awake()
    {
        InitializeAnimator();
        InitializePlayer();
        InitializeUIMessageManager();
    }

    /// <summary>
    /// Forsøger at finde og sætte <see cref="uiMessageManager"/>, hvis den ikke allerede
    /// er sat i Inspector. Prøver først via singleton, derefter via FindObjectOfType.
    /// </summary>
    protected void InitializeUIMessageManager()
    {
        if (uiMessageManager != null)
            return;

        // Prøv først via singleton, hvis projektet bruger det.
        if (UIMessageManager.Instance != null)
        {
            uiMessageManager = UIMessageManager.Instance;
        }
        else
        {
            // Fallback: søg i scenen.
            uiMessageManager = FindObjectOfType<UIMessageManager>();
        }

        if (uiMessageManager == null)
        {
            Debug.LogWarning("Chest: kunne ikke finde UIMessageManager i scenen.", this);
        }
    }

    private void Update()
    {
        TryOpenChest();
    }
    protected virtual bool CanOpen()
    {
        return true;
    }
    private void InitializeAnimator()
    {
        animator = GetComponentInChildren<Animator>();

        if (animator == null)
        {
            Debug.LogError("Chest: Fandt ingen Animator!", this);
            return;
        }

        cachedController = animator.runtimeAnimatorController;
        animator.runtimeAnimatorController = null; // ingen controller før vi åbner
    }

    private void InitializePlayer()
    {
        GameObject p = GameObject.FindWithTag("Player");

        if (p != null)
        {
            player = p.transform;
            playerInventory = p.GetComponent<PlayerInventory>();
            playerWeapon = p.GetComponent<PlayerWeapon>();
        }
        else
        {
            Debug.LogError("Chest: Ingen GameObject med tag 'Player' fundet!", this);
        }
    }

    private void TryOpenChest()
    {
        if (isOpened || player == null || animator == null) return;

        Vector3 toPlayer = player.position - transform.position;
        float distance = toPlayer.magnitude;
        if (distance > openDistance) return;

        Vector3 dirToPlayer = toPlayer.normalized;
        float dot = Vector3.Dot(transform.forward, dirToPlayer);
        if (dot <= frontDotThreshold) return;

        // 🔹 VIGTIGT: ekstra betingelser (fx “dragen skal være død”)
        if (!CanOpen()) return;

        StartOpenChest();
    }
    protected abstract IChestLoot CreateLoot();

    private void StartOpenChest()
    {
        if (isOpened) return;
        isOpened = true;

        StartCoroutine(OpenChestRoutine());
    }

    private IEnumerator OpenChestRoutine()
    {
     
        if (cachedController != null)
        {
            animator.runtimeAnimatorController = cachedController;
        }

        Debug.Log("Chest opened – controller sat automatisk!");

  
        if (!string.IsNullOrEmpty(openTriggerName))
        {
            animator.SetTrigger(openTriggerName);
        }

        yield return null;

        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        float animLength = stateInfo.length;

        // hvis du vil have “lidt mere” end animationen, kan du lægge lidt til:
        float waitTime = animLength + lootDelayAfterOpen;  
        yield return new WaitForSeconds(waitTime);

        // 5) nu gives loot
        IChestLoot loot = CreateLoot();
        if (loot != null)
        {
            loot.GiveItemsToPlayer(playerInventory, playerWeapon);
        }
    }

}

