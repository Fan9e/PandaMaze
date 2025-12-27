using UnityEngine;

public class Boss : Monster
{
    protected override ISpeechTaskPresenter CreatePresenter() => new FullScramblePresenter();

    /// <summary>
    /// Initialiserer bossen ved at sætte dens maksimale liv og angrebskraft
    /// og kalder Monster-basislogikken via base.Start().
    /// </summary>
    private void Start()
    {
        MaxHealth = 60;
        AttackPower = 20;
        base.Start();
    }

}
