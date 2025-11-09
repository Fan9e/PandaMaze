using UnityEngine;

public class OneHandSword : Weapon
{
    ///<summary>
    ///Beregner den skade et enhåndssværd gør.
    ///</summary>
    ///<returns>Den samlede mængde skade.</returns>
    public override int CalculateDamage()
    {
        int baseDamage = 10;
        int totalDamage = baseDamage;

        return totalDamage;
    }

    /// <summary>
    /// Kaldes én gang per frame af Unity og markerer,
    /// at våbnet er i gang med at angribe.
    /// </summary>
    void Update()
    {
        isAttacking = true;
    }
}
