using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI-komponent til at vise spillerens liv.
/// Opdaterer både numerisk tekst og healthbar baseret på en <see cref="Player"/>-komponent.
/// Hvis <see cref="player"/> ikke er sat i inspektøren forsøger scriptet at finde en på samme GameObject eller i scenen.
/// </summary>
public class PlayerHealthUI : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The Player component providing CurrentHealth and MaxHealth. If empty the script will try to find one on the GameObject or in the scene.")]
    [SerializeField] private Player player;

    /// <summary>
    /// TextMeshProUGUI brugt til at vise numerisk liv.
    /// </summary>
    [Tooltip("TextMeshProUGUI used to show numeric health (optional).")]
    [SerializeField] private TextMeshProUGUI healthText;

    /// <summary>
    /// Image brugt som health bar fill (Image.type bør være Filled).
    /// </summary>
    [Tooltip("Image used as the health bar fill (Image.type should be Filled).")]
    [SerializeField] private Image healthBar;

    /// <summary>
    /// Valgfri baggrundsimage for healthbaren.
    /// </summary>
    [Tooltip("Optional background image for the health bar.")]
    [SerializeField] private Image healthBarBG;

    /// <summary>
    /// Sidst kendte aktuelle liv (bruges for at undgå unødvendige opdateringer).
    /// </summary>
    private int _lastHealth = int.MinValue;

    /// <summary>
    /// Sidst kendte max liv (bruges for at undgå unødvendige opdateringer).
    /// </summary>
    private int _lastMax = int.MinValue;

    /// <summary>
    /// Initialiserer referencer og opdaterer UI med nuværende eller fallback-værdier.
    /// </summary>
    private void Start()
    {
        if (player == null)
        {
            player = GetComponent<Player>() ?? FindObjectOfType<Player>();
        }

        if (EnsureHasPlayer(player))
        {
            UpdateHealthUI(player.CurrentHealth, player.MaxHealth);
            return;
        }


        if (healthText != null)
        {
            healthText.text = "Ingen spiller kunne findes";
        }

        if (healthBar != null)
        {
            healthBar.enabled = false;
            healthBar.fillAmount = 0f;
        }

        if (healthBarBG != null)
        {
            healthBarBG.enabled = false;
        }            
    }

    /// <summary>
    /// Tjekker hver frame om spillerens liv eller max-liv er ændret og opdaterer UI ved ændring.
    /// </summary>
    private void Update()
    {
        if (!EnsureHasPlayer(player))
            return;

        if (player.CurrentHealth != _lastHealth || player.MaxHealth != _lastMax)
        {
            UpdateHealthUI(player.CurrentHealth, player.MaxHealth);
        }
    }

    /// <summary>
    /// Opdaterer UI til de angivne livsværdier.
    /// Metoden clamper aktuelle værdi til intervallet [0, max] og sørger for at max er mindst 1.
    /// </summary>
    /// <param name="current">Aktuelt liv; hvis udenfor intervallet bliver det clamped.</param>
    /// <param name="max">Max liv; hvis mindre end 1 sættes det til 1.</param>
    public void UpdateHealthUI(int current, int max)
    {
        int safeMax = Mathf.Max(1, max);
        int safeCurrent = Mathf.Clamp(current, 0, safeMax);

        if (healthText != null)
        {
            healthText.text = $"Health: {safeCurrent} / {safeMax}";
        }

        if (healthBar != null)
        {
            healthBar.fillAmount = (float)safeCurrent / safeMax;
        }

        _lastHealth = safeCurrent;
        _lastMax = safeMax;
    }

    /// <summary>
    /// Opdaterer UI ud fra den tilknyttede <see cref="Player"/>-komponent.
    /// Gør intet hvis ingen <see cref="player"/> er tildelt.
    /// </summary>
    public void UpdateHealthUI()
    {
        if (!EnsureHasPlayer(player))
            return;

        UpdateHealthUI(player.CurrentHealth, player.MaxHealth);
    }

    /// <summary>
    /// Sætter <see cref="player"/>-referencen og opdaterer UI umiddelbart.
    /// </summary>
    /// <param name="player">Den nye <see cref="Player"/>-instans der skal bruges til opdateringer.</param>
    public void SetPlayer(Player player)
    {
        this.player = player;

        if (!EnsureHasPlayer(this.player))
        {
            if (healthText != null)
            {
                healthText.text = "Ingen spiller kunne findes";
            }

            if (healthBar != null)
            {
                healthBar.enabled = false;
                healthBar.fillAmount = 0f;
            }

            if (healthBarBG != null)
            {
                healthBarBG.enabled = false;
            }

            return;
        }

        UpdateHealthUI();
    }

    /// <summary>
    /// Sikrer at der er en gyldig <see cref="Player"/>-referencen til rådighed.
    /// </summary>
    /// <param name="player">Den <see cref="Player"/>-instans der skal kontrolleres. Kan være null.</param>
    /// <returns>
    /// true hvis der er en gyldig <see cref="Player"/>; ellers false.
    /// </returns>
    protected virtual bool EnsureHasPlayer(Player player)
    {
        if (player != null) return true;

        Debug.LogWarning("Der er ingen player at kæmpe imod");
        return false;
    }
}
