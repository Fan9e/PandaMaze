using UnityEngine;

public class Cat : Monster
{
    /// <summary>
    /// Opretter og returnerer den presenter der bruges til at vise/afvikle speech-tasken.
    /// </summary>
    protected override ISpeechTaskPresenter CreatePresenter() => new OneWordScramblePresenter();

    /// <summary>
    /// Initialiserer 'katte'-monstret ved at sætte dens maksimale liv og angrebskraft
    /// og kalder Monster-basislogikken via base.Start().
    /// </summary>
    protected override void Start()
    {
        MaxHealth = 45;
        AttackPower = 10;
        base.Start();
    }

}
