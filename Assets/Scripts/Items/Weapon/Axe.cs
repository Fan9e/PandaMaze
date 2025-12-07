using UnityEngine;

public class Axe : Weapon
{
    [Header("Base Damage")]
    [SerializeField, Min(0)] private int baseDamage = 15;
    public override void ConfigureSocketTransform(Transform t)
    {
        t.localPosition = new Vector3(0.1f, 0.42f, 0.22f);
        t.localEulerAngles = new Vector3(-182.589f, 96.063f, -236.907f);
        t.localScale = new Vector3(4f, 4f, 4f);
    }
    ///<summary>
    ///Beregner den skade en økse gør.
    ///</summary>
    ///<returns>Den samlede mængde skade.</returns>
    public override int CalculateDamage()
    {
        int totalDamage = Mathf.Max(0, baseDamage);

        return totalDamage;
    }

}
