using UnityEngine;

[RequireComponent(typeof(Monster))]
[RequireComponent(typeof(SphereCollider))]
public class MonsterSpeechCombat : MonoBehaviour
{
    [SerializeField] private SpeechTaskUI speechTaskUI;
    [SerializeField] private int damagePerCorrect = 7;

    private Monster monster;
    private SphereCollider trigger;
    private bool fightActive;

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

        if (speechTaskUI != null && !speechTaskUI.IsActive)
        {
            speechTaskUI.ShowTask(); // samme sætning hver gang
        }
    }

    private void HandleTaskSuccess()
    {
        if (!fightActive) return;

        // Brug våbnets skade i stedet for et fast tal
        int damage = GetCurrentWeaponDamage();
        monster.TakeDamageOnly(damage);

        if (monster.CurrentHealth <= 0)
        {
            fightActive = false;
        }
        else
        {
            StartRound();
        }
    }


    private void HandleTaskFail()
    {
        if (!fightActive) return;

        monster.AttackPlayerOnly();

        if (monster.Player != null &&
            monster.Player.CurrentHealth > 0 &&
            monster.CurrentHealth > 0)
        {
            StartRound();
        }
        else
        {
            fightActive = false;
        }
    }
}
