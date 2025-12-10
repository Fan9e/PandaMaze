using UnityEngine;

[RequireComponent(typeof(Monster))]
[RequireComponent(typeof(SphereCollider))]
public abstract class MonsterSpeechCombatBase : MonoBehaviour
{
    /// <summary>
    /// Liv under eller lig med denne værdi betyder, at figureren er død.
    /// </summary>
    private const int DeadHealthThreshold = 0;

    /// <summary>
    /// Første gyldige index i sætning-arrayet.
    /// </summary>
    private const int FirstSentenceIndex = 0;

    [Header("Fælles tale-kamp")]
    [SerializeField] protected SpeechTaskUI speechTaskUI;

    /// <summary>
    /// Skade som spilleren giver for hver korrekt sagt sætning.
    /// </summary>
    [SerializeField] protected int damagePerCorrect = 10;

    /// <summary>
    /// Liste af alle sætninger, som monsteret bruger i tale-kampen.
    /// </summary>
    [TextArea]
    [SerializeField] protected string[] sentences;

    protected Monster monster;
    protected SphereCollider triggerCollider;

    protected bool isFightActive;
    protected bool isWaitingForResult;
    protected int currentSentenceIndex;

    /// <summary>
    /// Finder nødvendige komponenter (Monster + SphereCollider)  
    /// og sikrer, at trigger og SpeechTaskUI er sat op korrekt.
    /// </summary>
    protected virtual void Awake()
    {
        monster = GetComponent<Monster>();
        triggerCollider = GetComponent<SphereCollider>();
        triggerCollider.isTrigger = true;

        if (speechTaskUI == null)
            speechTaskUI = FindObjectOfType<SpeechTaskUI>();
    }

    /// <summary>
    /// Abonnerer på SpeechTaskUI-events, så vi kan reagere på  
    /// korrekt og forkert udtalt sætning under kampen.
    /// </summary>
    protected virtual void OnEnable()
    {
        if (speechTaskUI != null)
        {
            speechTaskUI.OnTaskCompleted.AddListener(HandleTaskSuccess);
            speechTaskUI.OnTaskFailed.AddListener(HandleTaskFail);
        }
    }

    /// <summary>
    /// Fjerner event-listeners når objektet disables,  
    /// så vi undgår memory leaks og fejl.
    /// </summary>
    protected virtual void OnDisable()
    {
        if (speechTaskUI != null)
        {
            speechTaskUI.OnTaskCompleted.RemoveListener(HandleTaskSuccess);
            speechTaskUI.OnTaskFailed.RemoveListener(HandleTaskFail);
        }
    }

    /// <summary>
    /// Starter en tale-kamp, når spilleren går ind i monsterets trigger.  
    /// Resetter kampens state og starter første runde.
    /// </summary>
    protected virtual void OnTriggerEnter(Collider otherCollider)
    {
        if (isFightActive)
            return;

        if (!otherCollider.TryGetComponent<Player>(out Player player))
            return;

        monster.Player = player;
        isFightActive = true;
        isWaitingForResult = false;
        currentSentenceIndex = FirstSentenceIndex;

        StartNewRound();
    }

    /// <summary>
    /// Starter en ny runde af kampen:
    /// - Tjekker om monster og spiller stadig lever  
    /// - Henter næste sætning  
    /// - Sender opgaven videre til UI’et  
    /// - Sætter state så vi venter på barnets svar
    /// </summary>
    protected void StartNewRound()
    {
        if (!isFightActive)
            return;

        if (monster.CurrentHealth <= DeadHealthThreshold)
        {
            isFightActive = false;
            return;
        }

        if (isWaitingForResult)
            return;

        if (speechTaskUI == null)
        {
            Debug.LogWarning($"{GetType().Name}: Mangler reference til SpeechTaskUI.");
            isFightActive = false;
            return;
        }

        isWaitingForResult = true;

        string sentence = GetNextSentence();
        ShowSentenceWithCorrectMode(sentence);
    }

    /// <summary>
    /// Vælger en tilfældig sætning fra listen,  
    /// så kampen føles varieret og uforudsigelig.
    /// </summary>
    protected virtual string GetNextSentence()
    {
        if (sentences == null || sentences.Length == 0)
            return null;

        int randomSentenceIndex = Random.Range(FirstSentenceIndex, sentences.Length);
        return sentences[randomSentenceIndex];
    }

    /// <summary>
    /// Viser sætningen på den korrekte måde (normal, scrambled, one-word).  
    /// Denne del varierer mellem monster-typerne og implementeres i subclasses.
    /// </summary>
    protected abstract void ShowSentenceWithCorrectMode(string sentence);

    /// <summary>
    /// Kaldes når barnet siger sætningen korrekt.  
    /// Giver spilleren skaden, viser sværd-animation,  
    /// og starter næste runde hvis monsteret stadig lever.
    /// </summary>
    protected virtual void HandleTaskSuccess()
    {
        if (!isFightActive || !isWaitingForResult)
            return;

        isWaitingForResult = false;

        PlayPlayerSwordAnimation();
        monster.TakeDamageOnly(damagePerCorrect);

        if (monster.CurrentHealth <= DeadHealthThreshold)
        {
            isFightActive = false;
            return;
        }

        currentSentenceIndex++;
        StartNewRound();
    }

    /// <summary>
    /// Kaldes når barnet siger noget forkert.  
    /// Monsteret slår spilleren.  
    /// Hvis begge stadig lever, gentages runden med samme sætning.
    /// </summary>
    protected virtual void HandleTaskFail()
    {
        if (!isFightActive || !isWaitingForResult)
            return;

        isWaitingForResult = false;

        monster.AttackPlayerOnly();

        bool playerIsAlive = monster.Player != null &&
                             monster.Player.CurrentHealth > DeadHealthThreshold;
        bool monsterIsAlive = monster.CurrentHealth > DeadHealthThreshold;

        if (playerIsAlive && monsterIsAlive)
        {
            StartNewRound();
        }
        else
        {
            isFightActive = false;
        }
    }

    /// <summary>
    /// Afspiller spillerens sværdangreb-animation  
    /// når en runde vindes.
    /// </summary>
    protected void PlayPlayerSwordAnimation()
    {
        if (monster.Player == null)
            return;

        PlayerWeapon playerWeapon = monster.Player.GetComponentInChildren<PlayerWeapon>();
        if (playerWeapon != null)
        {
            playerWeapon.PlayAttackAnimationOnly();
        }
    }
}
