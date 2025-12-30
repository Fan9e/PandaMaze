using UnityEngine;

public class Dragon : Monster
{
    /// <summary>
    /// Opretter og returnerer den presenter der bruges til at vise/afvikle speech-tasken.
    /// </summary>
    protected override ISpeechTaskPresenter CreatePresenter() => new NormalPresenter();
    /// <summary>
    /// Initialiserer dragen ved at sætte dens maksimale liv og angrebskraft
    /// og kalder Monster-basislogikken via base.Start().
    /// </summary>
    protected override void Start()
    {
        MaxHealth = 30;
        AttackPower = 5;
        base.Start();
    }

}
