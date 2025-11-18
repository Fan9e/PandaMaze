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
        
        if (!EnsureHasPlayer(Player)) return;
        ReceiveDamage(damageAmount);

        if (ShouldDie())
            OnDeath();   
        else
            GiveDamage(Player);

    }

    /// <summary>
    /// Afgør om fjenden skal dø ud fra sin nuværende livsmængde.
    /// </summary>
    /// <returns>True hvis <see cref="CurrentHealth"/> er mindre end eller lig 0; ellers false.</returns>
    protected virtual bool ShouldDie()
    {
        return CurrentHealth <= 0;
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
    protected virtual void OnDeath()
    {
        Debug.Log("Monster døde");
        if (Application.isPlaying)
            Destroy(gameObject);
        else
            DestroyImmediate(gameObject);
    }

}
