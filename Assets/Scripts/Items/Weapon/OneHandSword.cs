using UnityEngine;

public class OneHandSword : Weapon
{
    [Header("Base Damage")]
    [SerializeField, Min(0)] private int baseDamage = 10;
    
    public override void ConfigureSocketTransform(Transform t)
    {
        t.localPosition = new Vector3(0f, 0f, 0f);
        t.localEulerAngles = new Vector3(-176.485f, 93.837f, -258.082f);
        t.localScale = new Vector3(3f, 3f, 3f);
    }
    ///<summary>
    ///Beregner den skade et enhåndssværd gør.
    ///</summary>
    ///<returns>Den samlede mængde skade.</returns>
    public override int CalculateDamage()
    {
        int totalDamage = Mathf.Max(0, baseDamage);

        return totalDamage;
    }

}
