using UnityEngine;

/// <summary>
/// Scanner efter den nærmeste <see cref="Monster"/> i nærheden af spilleren og binder/viser
/// den tilknyttede <see cref="MonsterHealthUI"/> når en monster findes inden for <see cref="showDistance"/>.
/// </summary>
/// <remarks>
/// Klassen opdaterer kontrollen med et interval bestemt af <see cref="updateInterval"/> for at reducere CPU-belastning.
/// Hvis <see cref="player"/> eller <see cref="monsterHealthUI"/> ikke er sat via inspector, forsøger den at finde dem i scenen.
/// </remarks>
public class PlayerMonsterDetector : MonoBehaviour
{
    /// <summary>
    /// Valgfri reference til spilleren. Hvis den er tom prøver scriptet at finde en i scenen ved start.
    /// </summary>
    [Tooltip("Optional reference to the player. If left empty this script will try to find one in the scene.")]
    [SerializeField] private Player player;

    /// <summary>
    /// Reference til <see cref="MonsterHealthUI"/> som skal vises/skjules og bindes til det nærmeste monster.
    /// </summary>
    [Tooltip("Reference to the MonsterHealthUI that will show/hide and be bound to the nearest monster.")]
    [SerializeField] private MonsterHealthUI monsterHealthUI;

    /// <summary>
    /// Maksimal afstand (i enheder) hvor et monster betragtes som "nært".
    /// </summary>
    [Tooltip("Distance within which a monster is considered 'near'.")]
    [SerializeField] private float showDistance = 5f;

    /// <summary>
    /// Hvor ofte (i sekunder) nærmeste-monster-tjekket opdateres. Lavere = mere responsivt, højere = mindre CPU-forbrug.
    /// </summary>
    [Tooltip("How often (seconds) to refresh the nearest-monster check. Lower = more responsive, higher = less CPU cost.")]
    [SerializeField] private float updateInterval = 0.1f;

    /// <summary>
    /// Intern timer der tæller ned til næste opdatering.
    /// </summary>
    private float _timer;

    /// <summary>
    /// Initialisering: find manglende referencer og skjul <see cref="monsterHealthUI"/> hvis den er fundet.
    /// </summary>
    private void Start()
    {
        if (player == null)
            player = FindObjectOfType<Player>();

        if (monsterHealthUI == null)
            monsterHealthUI = FindObjectOfType<MonsterHealthUI>();

        if (monsterHealthUI != null)
            monsterHealthUI.Hide();
    }

    /// <summary>
    /// Kører hver frame: tæller ned med <see cref="_timer"/>, og ved udløb søger den efter det nærmeste monster
    /// inden for <see cref="showDistance"/>. Hvis et monster findes, vises det i <see cref="monsterHealthUI"/>,
    /// ellers skjules UI'en og monsterreferencen fjernes.
    /// </summary>
    private void Update()
    {
        if (player == null || monsterHealthUI == null) return;

        _timer -= Time.deltaTime;
        if (_timer > 0f) return;
        _timer = updateInterval;

        var monsters = FindObjectsOfType<Monster>();
        Monster nearest = null;
        float nearestSqr = showDistance * showDistance;
        Vector3 playerPos = player.transform.position;

        foreach (var monster in monsters)
        {
            float sqr = (monster.transform.position - playerPos).sqrMagnitude;
            if (sqr <= nearestSqr)
            {
                if (nearest == null || sqr < (nearest.transform.position - playerPos).sqrMagnitude)
                {
                    nearest = monster;
                }
            }
        }

        if (nearest != null)
        {
            monsterHealthUI.ShowForMonster(nearest);
        }
        else
        {
            monsterHealthUI.Hide();
            monsterHealthUI.SetMonster(null);
        }
    }
}
