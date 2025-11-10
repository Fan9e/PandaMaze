using UnityEngine;

public class Dragon : Monster
{
    /// <summary>
    /// Sætter CurrentHealth til MaxHealth, når objektet starter.
    /// </summary>
    void Start()
    {   
        MaxHealth = 30;
        AttackPower = 10;
        CurrentHealth = MaxHealth;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
