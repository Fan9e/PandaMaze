using UnityEngine;

public class Cat : Monster
{
    /// <summary>
    /// Initialiserer 'katte'-monstret ved at sætte dens maksimale liv og angrebskraft
    /// og kalder Monster-basislogikken via base.Start().
    /// </summary>
    private void Start()
    {
        MaxHealth = 45;
        AttackPower = 10;
        base.Start();
    }

}
