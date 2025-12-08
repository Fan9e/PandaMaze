using UnityEngine;

public class TwoHandSword : Weapon
{
    [Header("Base Damage")]
    [SerializeField, Min(0)] private int baseDamage = 30;
    public override void ConfigureSocketTransform(Transform t)
    {
        t.localPosition = new Vector3(-0.11f, 0.689f, 1.53f);
        t.localEulerAngles = new Vector3(-32.737f, 109.432f, 59.494f);
        t.localScale = new Vector3(4f, 4f, 4f);
    }
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