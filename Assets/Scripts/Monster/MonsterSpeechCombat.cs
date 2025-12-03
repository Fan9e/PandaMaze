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

        monster.TakeDamageOnly(damagePerCorrect);

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
