using UnityEngine;

[RequireComponent(typeof(Monster))]
[RequireComponent(typeof(SphereCollider))]
public class CatSpeechCombat : MonoBehaviour
{
    [Header("Opgaver (Cat)")]
    [SerializeField] private SpeechTaskUI speechTaskUI;

    [Tooltip("Skade pr. rigtig sætning (tale-skade).")]
    [SerializeField] private int damagePerCorrect = 15;

    [Tooltip("Liste af rigtige sætninger for denne kat-kamp.")]
    [TextArea]
    [SerializeField] private string[] sentences;

    private Monster monster;
    private SphereCollider trigger;

    private bool fightActive;        // Er vi i kamp mod denne kat?
    private bool waitingForResult;   // Venter vi på, at spilleren siger noget?
    private int currentSentenceIndex;

    private void Awake()
    {
        monster = GetComponent<Monster>();
        trigger = GetComponent<SphereCollider>();
        trigger.isTrigger = true;

        if (speechTaskUI == null)
            speechTaskUI = FindObjectOfType<SpeechTaskUI>();
    }

    private void OnEnable()
    {
        if (speechTaskUI != null)
        {
            speechTaskUI.OnTaskCompleted.AddListener(HandleTaskSuccess);
            speechTaskUI.OnTaskFailed.AddListener(HandleTaskFail);
        }
    }

    private void OnDisable()
    {
        if (speechTaskUI != null)
        {
            speechTaskUI.OnTaskCompleted.RemoveListener(HandleTaskSuccess);
            speechTaskUI.OnTaskFailed.RemoveListener(HandleTaskFail);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Hvis vi allerede er i kamp, så ignorer
        if (fightActive) return;

        // Kun reagere på Player
        if (!other.TryGetComponent<Player>(out var player))
            return;

        monster.Player = player;
        fightActive = true;
        waitingForResult = false;
        currentSentenceIndex = 0;

        StartRound();
    }

    /// <summary>
    /// Starter en ny runde (viser en ny opgave), hvis både kat og spiller er i live.
    /// </summary>
    private void StartRound()
    {
        // Hvis monster/kat allerede er død → afslut kamp
        if (monster.CurrentHealth <= 0)
        {
            fightActive = false;
            return;
        }

        // Hvis vi stadig venter på resultat fra mikrofonen → gør ingenting
        if (waitingForResult)
            return;

        if (speechTaskUI == null)
        {
            Debug.LogWarning("CatSpeechCombat: Mangler reference til SpeechTaskUI.");
            fightActive = false;
            return;
        }

        waitingForResult = true;

        // Hvis der er defineret sætninger til denne kat
        if (sentences != null && sentences.Length > 0)
        {
            // Gå sekventielt igennem listen og wrap rundt
            int index = currentSentenceIndex % sentences.Length;
            string sentence = sentences[index];

            // BRUG "ÉT ORD FORKERT"-opgave:
            speechTaskUI.ShowOneWordScrambledTask(sentence);
        }
        else
        {
            // Fallback: brug normal opgave
            speechTaskUI.ShowTask();
        }
    }

    ///// <summary>
    ///// Kaldes når spilleren har sagt den rigtige sætning.
    ///// </summary>
    //private void HandleTaskSuccess()
    //{
    //    if (!fightActive || !waitingForResult)
    //        return;

    //    waitingForResult = false;

    //    // Brug tale-skade
    //    monster.TakeDamageOnly(damagePerCorrect);

    //    // Er katten død nu?
    //    if (monster.CurrentHealth <= 0)
    //    {
    //        fightActive = false;
    //        return;
    //    }

    //    // Katten lever stadig → gå videre til næste sætning
    //    currentSentenceIndex++;
    //    StartRound();
    //}

    private void HandleTaskSuccess()
    {
        if (!fightActive || !waitingForResult)
            return;

        waitingForResult = false;

        // Sværd-animation
        if (monster.Player != null)
        {
            var playerWeapon = monster.Player.GetComponentInChildren<PlayerWeapon>();
            if (playerWeapon != null)
            {
                playerWeapon.PlayAttackAnimationOnly();
            }
        }

        // Tale-skade
        monster.TakeDamageOnly(damagePerCorrect);

        if (monster.CurrentHealth <= 0)
        {
            fightActive = false;
            return;
        }

        currentSentenceIndex++;
        StartRound();
    }

    /// <summary>
    /// Kaldes når spilleren siger noget forkert.
    /// </summary>
    private void HandleTaskFail()
    {
        if (!fightActive || !waitingForResult)
            return;

        waitingForResult = false;

        // Katten angriber spilleren
        monster.AttackPlayerOnly();

        // Hvis begge er i live → prøv samme sætning igen
        if (monster.Player != null &&
            monster.Player.CurrentHealth > 0 &&
            monster.CurrentHealth > 0)
        {
            StartRound(); // samme currentSentenceIndex → samme sætning igen
        }
        else
        {
            fightActive = false;
        }
    }

   
   
}
