using UnityEngine;

public class Axe : Weapon
{
    [Header("Base Damage")]
    [SerializeField, Min(0)] private int baseDamage = 15;
    ///<summary>
    ///Beregner den skade en økse gør.
    ///</summary>
    ///<returns>Den samlede mængde skade.</returns>
    public override int CalculateDamage()
    {
        int totalDamage = Mathf.Max(0, baseDamage);

        return totalDamage;
    }

    /// <summary>
    /// Kaldes én gang per frame af Unity og markerer,
    /// at øksen er i gang med at angribe.
    /// </summary>
    private void Update()
    {
        isAttacking = true;
    }
}
