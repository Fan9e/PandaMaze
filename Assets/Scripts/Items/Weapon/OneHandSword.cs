using UnityEngine;

public class OneHandSword : Weapon
{
    [Header("Base Damage")]
    [SerializeField, Min(0)] private int baseDamage = 10;
    ///<summary>
    ///Beregner den skade et enhåndssværd gør.
    ///</summary>
    ///<returns>Den samlede mængde skade.</returns>
    public override int CalculateDamage()
    {
      
        int totalDamage = Mathf.Max(0, baseDamage);

        return totalDamage;
    }

    /// <summary>
    /// Kaldes én gang per frame af Unity og markerer,
    /// at våbnet er i gang med at angribe.
    /// </summary>
    private void Update()
    {
        isAttacking = true;
    }
}
