using UnityEngine;

public class Player : MonoBehaviour
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

    // Update is called once per frame
    void Update()
    {
        
    }
}
