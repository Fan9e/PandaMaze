using UnityEngine;

public class Monster : MonoBehaviour
{

    public int MaxHealth = 100;
    public int CurrentHealth;
    /// <summary>
    /// Sætter CurrentHealth til MaxHealth, når objektet starter.
    /// </summary>
    void Start()
    {
        CurrentHealth = MaxHealth;
    }
    /// <summary>
    /// Påfører dette monster skade og opdaterer dets nuværende liv.
    /// Livet begrænses til området mellem 0 og MaxHealth.
    /// </summary>
    /// <param name="damageAmount">Mængden af skade, der skal påføres.</param>
    public void ReceiveDamage(int damageAmount)
    {
        if (damageAmount < 0) return;

        CurrentHealth -= damageAmount;

        CurrentHealth = Mathf.Clamp(CurrentHealth, 0, MaxHealth);

        Debug.Log(" HP: " + CurrentHealth);
    }

    //TODO: I en anden branch bliver implementeringen af Fight lavet færdig.
    /// <summary>
    /// Monsteret modtager skade
    /// </summary>
    /// <param name="damageAmount">Mængden af skade, som dette monster modtager i starten af kampen.</param>
    public void Fight(int damageAmount)
    {
        ReceiveDamage(damageAmount);
    }

}
