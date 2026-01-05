using UnityEngine;

public class OneHandSword : Weapon
{
    [Header("Base Damage")]
    [SerializeField, Min(0)] private int baseDamage = 10;
    [Header("Socket Offset")]
    /// <summary>
    /// Lokal position i forhold til weapon-socket (WeaponPivot).
    /// </summary>
    public Vector3 socketLocalPosition = new Vector3(0f, 0f, 0f);

    /// <summary>
    /// Lokal rotation (Euler-angles) i forhold til weapon-socket.
    /// </summary>
    public Vector3 socketLocalEulerAngles = new Vector3(-176.485f, 93.837f, -258.082f);

    /// <summary>
    /// Lokal skalering i forhold til weapon-socket.
    /// </summary>
    public Vector3 socketLocalScale = new Vector3(3f, 3f, 3f);

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
    ///Beregner den skade et enhåndssværd gør.
    ///</summary>
    ///<returns>Den samlede mængde skade.</returns>
    public override int CalculateDamage()
    {
        int totalDamage = Mathf.Max(0, baseDamage);

        return totalDamage;
    }

}
