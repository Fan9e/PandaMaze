using UnityEngine;

public abstract class Monster : MonoBehaviour
{
    [SerializeField] private Player player;   
    public Player Player                     
    {
        get => player;
        set => player = value;
    }

    [SerializeField] public int MaxHealth { get; set; } = 0;
    [SerializeField] public int CurrentHealth;
    [SerializeField] public int AttackPower { get; set; } = 0;

    /// <summary>
    /// Sætter CurrentHealth til MaxHealth, når objektet starter.
    /// </summary>
    protected virtual void Start()
    {
        CurrentHealth = MaxHealth;
    }

    /// <summary>
    /// Starter en kampsekvens mellem dette monster og spilleren.
    /// Monsteret modtager skade, tjekker om det dør og forsøger derefter at give skade til spilleren. 
    /// </summary>
    /// <param name="damageAmount">Mængden af skade, som dette monster modtager i starten af kampen.</param>
    public void Fight(int damageAmount)
    {
        
        if (!EnsureHasPlayer(player)) return;
        ReceiveDamage(damageAmount);
        Die();
        GiveDamage(player);

    }

    /// <summary>
    /// Påfører dette monster skade og opdaterer dets nuværende liv.
    /// Livet begrænses til området mellem 0 og MaxHealth.
    /// </summary>
    /// <param name="damageAmount">Mængden af skade, der skal påføres.</param>
    protected void ReceiveDamage(int damageAmount)
    {
        if (damageAmount < 0) return;

        CurrentHealth -= damageAmount;

        CurrentHealth = Mathf.Clamp(CurrentHealth, 0, MaxHealth);

        Debug.Log(" HP: " + CurrentHealth);
    }
    /// <summary>
    /// Giver skade til spilleren baseret på dette monsters angrebsstyrke.
    /// </summary>
    protected virtual void GiveDamage(Player player)
    {
        if (CurrentHealth <= 0) return;
        player.CurrentHealth -= AttackPower;  
    }

    /// <summary>
    /// Sikrer, at der er tilknyttet en spiller til monsteret.
    /// Logger en advarsel, hvis der ikke er nogen spiller.
    /// </summary>
    /// <returns>True, hvis der er en spiller tilknyttet; ellers false.</returns>
    protected virtual bool EnsureHasPlayer(Player player)
    {
        if (player != null) return true;

        Debug.LogWarning("Der er ingen player at kæmpe imod");
        return false;
    }

    /// <summary>
    /// Håndterer monsterets død, når det ikke har mere liv.
    /// Destruerer objektet, hvis dets liv er 0 eller derunder
    /// </summary>
    protected virtual void Die()
    {
        if (CurrentHealth > 0) return;
        Debug.Log("Monster døde");
        if (Application.isPlaying)
            Destroy(gameObject);
        else
            DestroyImmediate(gameObject);
    }

}
