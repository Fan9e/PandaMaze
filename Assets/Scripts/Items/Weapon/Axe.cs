using UnityEngine;

public class Axe : Weapon
{
    [Header("Base Damage")]
    [SerializeField, Min(0)] private int baseDamage = 15;

    [Header("Socket Offset")]
    /// <summary>
    /// Lokal position i forhold til weapon-socket (WeaponPivot).
    /// </summary>
    public Vector3 socketLocalPosition = new Vector3(0.1f, 0.42f, 0.22f);

    /// <summary>
    /// Lokal rotation (Euler-angles) i forhold til weapon-socket.
    /// </summary>
    public Vector3 socketLocalEulerAngles = new Vector3(-182.589f, 96.063f, -236.907f);

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
    ///Beregner den skade en økse gør.
    ///</summary>
    ///<returns>Den samlede mængde skade.</returns>
    public override int CalculateDamage()
    {
        int totalDamage = Mathf.Max(0, baseDamage);

        return totalDamage;
    }

}
