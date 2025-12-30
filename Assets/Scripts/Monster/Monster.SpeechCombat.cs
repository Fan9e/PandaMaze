using UnityEngine;


/// <summary>
/// Partial monster-klasse, der håndterer speech-baseret kampflow.
/// </summary>
public partial class Monster
{
    [Header("Speech Combat")]
    /// <summary>
    /// UI’et der viser speech-opgaven og sender succes/fejl events tilbage.
    /// </summary>
    [SerializeField] private SpeechTaskUI speechTaskUI;

    /// <summary>
    /// Mulige sætninger der kan blive valgt tilfældigt og vist i UI’et.
    /// </summary>
    [TextArea][SerializeField] private string[] sentences;

    /// <summary>
    /// Trigger-collideren der starter kampen, når spilleren går ind i området.
    /// </summary>
    private SphereCollider triggerCollider;

    /// <summary>
    /// Angiver om fight/flowet er aktivt (spilleren er engageret i speech-kampen).
    /// </summary>
    private bool isFightActive;

    /// <summary>
    /// Angiver om vi venter på et resultat fra speech-tasken (succes eller fejl).
    /// </summary>
    private bool isWaitingForResult;

    /// <summary>
    /// Den sætning, som er aktiv i den nuværende runde.
    /// </summary>
    private string currentSentence;

    /// <summary>
    /// Fabriksmetode der skal returnere en presenter, som kan vise en speech-task i UI’et.
    /// Implementeres i en afledt klasse.
    /// </summary>
    /// <returns>En presenter der implementerer <see cref="ISpeechTaskPresenter"/>.</returns>
    protected abstract ISpeechTaskPresenter CreatePresenter();

    /// <summary>
    /// Den konkrete presenter-instans som bruges til at vise opgaven.
    /// </summary>
    private ISpeechTaskPresenter presenter;

    /// <summary>
    /// Unity-callback der kører når objektet oprettes.
    /// Finder og konfigurerer trigger-collideren, finder UI hvis det mangler,
    /// og opretter presenter via <see cref="CreatePresenter"/>.
    /// </summary>
    protected virtual void Awake()
    {
        triggerCollider = GetComponent<SphereCollider>();
        triggerCollider.isTrigger = true;

        if (speechTaskUI == null)
            speechTaskUI = FindFirstObjectByType<SpeechTaskUI>();

        presenter = CreatePresenter();
    }

    /// <summary>
    /// Unity-callback når komponenten bliver enabled.
    /// Tilmeld lyttere til UI’ets events for succes og fejl.
    /// </summary>
    protected virtual void OnEnable()
    {
        if (speechTaskUI == null) return;
        speechTaskUI.OnTaskCompleted.AddListener(HandleTaskSuccess);
        speechTaskUI.OnTaskFailed.AddListener(HandleTaskFail);
    }

    /// <summary>
    /// Unity-callback når komponenten bliver disabled.
    /// Afmeld lyttere fra UI’ets events for at undgå leaks/dobbelt callbacks.
    /// </summary>
    protected virtual void OnDisable()
    {
        if (speechTaskUI == null) return;
        speechTaskUI.OnTaskCompleted.RemoveListener(HandleTaskSuccess);
        speechTaskUI.OnTaskFailed.RemoveListener(HandleTaskFail);
    }

    /// <summary>
    /// Unity-callback når noget går ind i triggeren.
    /// Starter en ny speech-kamp hvis det er en <see cref="Player"/> og der ikke allerede kæmpes.
    /// </summary>
    /// <param name="other">Collideren der gik ind i triggeren.</param>
    protected virtual void OnTriggerEnter(Collider other)
    {
        if (isFightActive) return;
        if (!other.TryGetComponent<Player>(out var player)) return;

        Player = player;
        isFightActive = true;
        isWaitingForResult = false;

        StartNewRound(pickNewSentence: true);
    }

    /// <summary>
    /// Starter en ny runde i speech-kampen.
    /// Vælger evt. en ny sætning og viser opgaven i UI’et, hvis vi må fortsætte.
    /// </summary>
    /// <param name="pickNewSentence">
    /// Hvis true vælges en ny sætning; ellers genbruges den nuværende (hvis den findes).
    /// </param>
    private void StartNewRound(bool pickNewSentence)
    {
        if (!isFightActive) return;
        if (CurrentHealth <= 0) { EndFight(); return; }

        if (isWaitingForResult) return;

        if (speechTaskUI == null)
        {
            Debug.LogWarning($"{name}: Mangler SpeechTaskUI.");
            isFightActive = false;
            return;
        }

        isWaitingForResult = true;

        if (pickNewSentence || string.IsNullOrEmpty(currentSentence))
            currentSentence = GetNextSentence();

        speechTaskUI.gameObject.SetActive(true);
        presenter?.Show(speechTaskUI, currentSentence);
    }

    /// <summary>
    /// Vælger næste sætning til speech-opgaven.
    /// Returnerer en fallback-tekst hvis der ikke er sat sætninger op.
    /// </summary>
    /// <returns>En tilfældig sætning fra <see cref="sentences"/> eller en fallback-tekst.</returns>
    private string GetNextSentence()
    {
        if (sentences == null || sentences.Length == 0) return "Denne sætning skal hjælp med at udtale orden";
        return sentences[Random.Range(0, sentences.Length)];
    }

    /// <summary>
    /// Kaldes når speech-opgaven er gennemført korrekt.
    /// Lader spilleren angribe monsteret (via PlayerWeapon hvis muligt), ellers falder tilbage til standard skade.
    /// Starter derefter en ny runde eller slutter kampen hvis monsteret dør.
    /// </summary>
    private void HandleTaskSuccess()
    {
        if (!isFightActive || !isWaitingForResult) return;
        isWaitingForResult = false;

        if (Player != null)
        {
            var playerWeapon = Player.GetComponentInChildren<PlayerWeapon>();
            if (playerWeapon != null)
            {
                playerWeapon.AttackSpecificMonster(this);
            }
            else
            {
                Debug.LogWarning("PlayerWeapon blev ikke fundet på Player.");
                Fight(10); 
            }
        }
        else
        {
            Fight(10);
        }

        if (CurrentHealth <= 0)
        {
            EndFight();
            return;
        }

        StartNewRound(pickNewSentence: true);
    }

    /// <summary>
    /// Kaldes når speech-opgaven fejler.
    /// Anvender “fejl”-konsekvens (her Fight(0)), og afgør om kampen fortsætter
    /// med samme sætning eller slutter afhængigt af om spiller/monster er i live.
    /// </summary>
    private void HandleTaskFail()
    {
        if (!isFightActive || !isWaitingForResult) return;
        isWaitingForResult = false;

        Fight(0);

        bool playerAlive = Player != null && Player.CurrentHealth > 0;
        bool monsterAlive = CurrentHealth > 0;

        if (playerAlive && monsterAlive)
            StartNewRound(pickNewSentence: false);
        else
            EndFight();
    }

    /// <summary>
    /// Afslutter speech-kampen og rydder state.
    /// Skjuler UI’et og nulstiller den aktuelle sætning.
    /// </summary>
    private void EndFight()
    {
        isFightActive = false;
        isWaitingForResult = false;

        if (speechTaskUI != null)
            speechTaskUI.gameObject.SetActive(false);

        currentSentence = null;
    }

}
