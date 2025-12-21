using UnityEngine;

public class TwoHandSword : Weapon
{
    [Header("Base Damage")]
    [SerializeField, Min(0)] private int baseDamage = 30;
    ///<summary>
    ///Beregner den skade et tohåndssværd gør.
    ///</summary>
    ///<returns>Den samlede mængde skade.</returns>
    public override int CalculateDamage()
    {
        int totalDamage = Mathf.Max(0, baseDamage);

        return totalDamage;
    }

}