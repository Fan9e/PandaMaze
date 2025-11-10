using UnityEngine;

public class Monster : MonoBehaviour
{
    public Player player;
    public int MaxHealth { get; protected set; } = 0;
    public int CurrentHealth;
    public int AttackPower { get; protected set; } = 0;

    /// <summary>
    /// Sætter CurrentHealth til MaxHealth, når objektet starter.
    /// </summary>
    void Start()
    {
        CurrentHealth = MaxHealth;
    }
    public void Fight(int damageAmount)
    {
        NullPlayer();
        ReceiveDamage(damageAmount);
        Die();
        GiveDamage();


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
    protected virtual void GiveDamage()
    {
        Debug.Log("Monster giver skade.");

        if (player == null) return;
        
            player.CurrentHealth -= AttackPower;
            Debug.Log("Player HP: " + player.CurrentHealth);
       
    }
    protected virtual void NullPlayer()
    {
        
        if (player == null) return;
        player.transform.position = Vector3.zero;
        Debug.Log("Monster nulstillede spillerens position.");
    }
    protected virtual void Die()
    {
        if (CurrentHealth > 0) return;
        Debug.Log("Monster døde.");
        Destroy(gameObject);
    }



}
