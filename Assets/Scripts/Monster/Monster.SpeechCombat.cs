using UnityEngine;

public partial class Monster : MonoBehaviour
{
    [Header("Speech Combat")]
    [SerializeField] private SpeechTaskUI speechTaskUI;
    [TextArea][SerializeField] private string[] sentences;

    private SphereCollider triggerCollider;

    private bool isFightActive;
    private bool isWaitingForResult;
    private string currentSentence;

    protected abstract ISpeechTaskPresenter CreatePresenter();
    private ISpeechTaskPresenter presenter;

    protected virtual void Awake()
    {
        triggerCollider = GetComponent<SphereCollider>();
        triggerCollider.isTrigger = true;

        if (speechTaskUI == null)
            speechTaskUI = FindFirstObjectByType<SpeechTaskUI>();

        presenter = CreatePresenter();
    }

    protected virtual void OnEnable()
    {
        if (speechTaskUI == null) return;
        speechTaskUI.OnTaskCompleted.AddListener(HandleTaskSuccess);
        speechTaskUI.OnTaskFailed.AddListener(HandleTaskFail);
    }

    protected virtual void OnDisable()
    {
        if (speechTaskUI == null) return;
        speechTaskUI.OnTaskCompleted.RemoveListener(HandleTaskSuccess);
        speechTaskUI.OnTaskFailed.RemoveListener(HandleTaskFail);
    }

    protected virtual void OnTriggerEnter(Collider other)
    {
        if (isFightActive) return;
        if (!other.TryGetComponent<Player>(out var player)) return;

        Player = player;
        isFightActive = true;
        isWaitingForResult = false;

        StartNewRound(pickNewSentence: true);
    }

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

    private string GetNextSentence()
    {
        if (sentences == null || sentences.Length == 0) return "Denne sætning skal hjælp med at udtale orden";
        return sentences[Random.Range(0, sentences.Length)];
    }

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

    private void EndFight()
    {
        isFightActive = false;
        isWaitingForResult = false;

        if (speechTaskUI != null)
            speechTaskUI.gameObject.SetActive(false);

        currentSentence = null;
    }

}
