using UnityEngine;

public class TwoHandSword : Weapon
{
    [Header("Base Damage")]
    [SerializeField, Min(0)] private int baseDamage = 30;
    /// <summary>
    /// Lokal position i forhold til weapon-socket (WeaponPivot).
    /// </summary>
    public Vector3 socketLocalPosition = new Vector3(-0.11f, 0.689f, 1.53f);

    /// <summary>
    /// Lokal rotation (Euler-angles) i forhold til weapon-socket.
    /// </summary>
    public Vector3 socketLocalEulerAngles = new Vector3(-32.737f, 109.432f, 59.494f);

    /// <summary>
    /// Lokal skalering i forhold til weapon-socket.
    /// </summary>
    public Vector3 socketLocalScale = new Vector3(4f, 4f, 4f);
    /// <summary>
    /// Placerer og roterer våbnet korrekt i weapon-socketen ved at sætte lokale offset-værdier
    /// (position/rotation/scale) relativt til socketTransform.
    /// </summary>
    /// <param name="socketTransform">Socket/pivot som våbnet er parentet til.</param>
    public override void ConfigureSocketTransform(Transform socketTransform)
    {
        socketTransform.localPosition = socketLocalPosition;
        socketTransform.localEulerAngles = socketLocalEulerAngles;
        socketTransform.localScale = socketLocalScale;
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