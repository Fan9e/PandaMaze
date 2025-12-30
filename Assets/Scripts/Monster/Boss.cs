using UnityEngine;

public class Boss : Monster
{
    /// <summary>
    /// Opretter og returnerer den presenter der bruges til at vise/afvikle speech-tasken.
    /// </summary>
    protected override ISpeechTaskPresenter CreatePresenter() => new FullScramblePresenter();

    /// <summary>
    /// Initialiserer bossen ved at sætte dens maksimale liv og angrebskraft
    /// og kalder Monster-basislogikken via base.Start().
    /// </summary>
    protected override void Start()
    {
        MaxHealth = 60;
        AttackPower = 20;
        base.Start();
    }

}
