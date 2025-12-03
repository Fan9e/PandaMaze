using UnityEngine;

[RequireComponent(typeof(FaceCamera))]
public abstract class Monster : MonoBehaviour
{
    [SerializeField] private Player player;
    [SerializeField] private int maxHealth = 0;
    [SerializeField] private int currentHealth = 0;
    [SerializeField] private int attackPower = 0;

    public Player Player
    {
        get => player;
        set => player = value;
    }

    public int MaxHealth
    {
        get => maxHealth;
        set => maxHealth = value;
    }

    public int CurrentHealth
    {
        get => currentHealth;
        set => currentHealth = value;
    }

    public int AttackPower
    {
        get => attackPower;
        set => attackPower = value;
    }

    protected virtual void Start()
    {
        CurrentHealth = MaxHealth;
    }

    // GAMMEL – deaktiveret
    public void Fight(int damageAmount)
    {
        Debug.Log("Monster.Fight() kaldt, men er deaktiveret (tale-kamp bruges).");
    }

    // NY: kun skade på monster
    public void TakeDamageOnly(int damageAmount)
    {
        if (!EnsureHasPlayer()) return;

        ReceiveDamage(damageAmount);

        if (ShouldDie())
            OnDeath();
    }

    // NY: kun skade på spiller
    public void AttackPlayerOnly()
    {
        if (!EnsureHasPlayer()) return;

        if (AttackPower <= 0)
        {
            Debug.LogWarning($"{name} har AttackPower 0 – ingen skade givet.");
            return;
        }

        Debug.Log($"{name} angriber {Player.name} for {AttackPower} HP");
        GiveDamage(Player);
    }

    protected virtual bool ShouldDie()
    {
        return CurrentHealth <= 0;
    }

    protected void ReceiveDamage(int damageAmount)
    {
        if (damageAmount < 0) return;

        CurrentHealth -= damageAmount;
        CurrentHealth = Mathf.Clamp(CurrentHealth, 0, MaxHealth);

        Debug.Log($"{name} HP: {CurrentHealth}/{MaxHealth}");
    }

    protected virtual void GiveDamage(Player player)
    {
        player.CurrentHealth -= AttackPower;
        Debug.Log($"{player.name} HP: {player.CurrentHealth}");
    }

    protected virtual bool EnsureHasPlayer()
    {
        if (Player != null)
            return true;

        Player = FindFirstObjectByType<Player>();

        if (Player == null)
        {
            Debug.LogWarning("Monster kunne ikke finde nogen Player at kæmpe imod.", this);
            return false;
        }

        return true;
    }

    protected virtual void OnDeath()
    {
        Debug.Log($"{name} døde");
        if (Application.isPlaying)
            Destroy(gameObject);
        else
            DestroyImmediate(gameObject);
    }
}
