using UnityEngine;

[RequireComponent(typeof(Monster))]
[RequireComponent(typeof(SphereCollider))]
public class MonsterSpeechCombat : MonoBehaviour
{
    [Header("Opgaver")]
    [SerializeField] private SpeechTaskUI speechTaskUI;

    [Tooltip("Fallback-skade hvis vi ikke finder et Weapon på spilleren.")]
    [SerializeField] private int damagePerCorrect = 7;

    [Tooltip("Liste af sætninger i rækkefølge for denne kamp.")]
    [TextArea]
    [SerializeField] private string[] sentences;

    private Monster monster;
    private SphereCollider trigger;
    private bool fightActive;
    private bool waitingForResult;
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
        if (fightActive) return;

        if (!other.TryGetComponent<Player>(out var player))
            return;

        monster.Player = player;
        fightActive = true;
        waitingForResult = false;
        currentSentenceIndex = 0;   // ← starter med første sætning
        StartRound();
    }

    private int GetCurrentWeaponDamage()
    {
        // Hvis der ikke er sat en spiller på monsteret, brug fallback
        if (monster.Player == null)
            return damagePerCorrect;

        // Prøv at finde et Weapon-script på spilleren eller dens children
        Weapon weapon = monster.Player.GetComponentInChildren<Weapon>();

        if (weapon != null)
        {
            // Brug våbnets egen skadeberegning
            return weapon.CalculateDamage();
        }

        // Hvis vi ikke fandt noget våben, falder vi tilbage til det faste tal
        return damagePerCorrect;
    }

    private void StartRound()
    {
        if (monster.CurrentHealth <= 0)
        {
            fightActive = false;
            return;
        }

        // Allerede startet en opgave, venter på svar
        if (waitingForResult)
            return;

        if (speechTaskUI != null && !speechTaskUI.IsActive)
        {
            waitingForResult = true;      // nu venter vi på et resultat

            // Hvis vi har defineret sætninger til dette monster
            if (sentences != null && sentences.Length > 0)
            {
                int index = Mathf.Clamp(currentSentenceIndex, 0, sentences.Length - 1);
                string sentence = sentences[index];
                speechTaskUI.ShowTask(sentence);
            }
            else
            {
                // fallback: brug standard-sætningen fra SpeechTaskUI
                speechTaskUI.ShowTask();
            }
        }
    }


    private void HandleTaskSuccess()
    {
        if (!fightActive || !waitingForResult)
            return;

        waitingForResult = false;   // resultat modtaget

        int damage = GetCurrentWeaponDamage();
        monster.TakeDamageOnly(damage);

        if (monster.CurrentHealth <= 0)
        {
            // Monster dødt → kamp slut
            fightActive = false;
            return;
        }

        // Monster lever stadig → gå videre til næste opgave
        currentSentenceIndex++;
        StartRound();
    }

    private void HandleTaskFail()
    {
        if (!fightActive || !waitingForResult)
            return;

        waitingForResult = false;   // resultat modtaget

        monster.AttackPlayerOnly();

        if (monster.Player != null &&
            monster.Player.CurrentHealth > 0 &&
            monster.CurrentHealth > 0)
        {
            // Samme sætning igen, fordi vi IKKE har ændret currentSentenceIndex
            StartRound();
        }
        else
        {
            fightActive = false;
        }
    }

}
