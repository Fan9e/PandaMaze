using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI-komponent der viser liv (health) for et <see cref="Monster"/>.
/// Opdaterer valgfri tekst og en fyldt Image (health bar) baseret på Monster.CurrentHealth og Monster.MaxHealth.
/// </summary>
public class MonsterHealthUI : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The Monster component providing CurrentHealth and MaxHealth. If empty the UI will remain hidden until SetMonster/ShowForMonster is called.")]
    [SerializeField]
    /// <summary>
    /// Referencen til Monster-komponenten som leverer CurrentHealth og MaxHealth.
    /// Hvis denne er tom, forbliver UI'en skjult indtil <see cref="SetMonster(Monster)"/> eller <see cref="ShowForMonster(Monster)"/> kaldes.
    /// </summary>
    private Monster monster;

    [Tooltip("TextMeshProUGUI used to show numeric health (optional).")]
    [SerializeField]
    /// <summary>
    /// Valgfri TextMeshProUGUI som viser numerisk liv (f.eks. "50 / 100").
    /// Hvis <c>null</c> vises kun health baren (hvis sat).
    /// </summary>
    private TextMeshProUGUI healthText;

    [Tooltip("Image used as the health bar fill (Image.type should be Filled).")]
    [SerializeField]
    /// <summary>
    /// Image der bruges som health bar-fill. Image.type bør være sat til Filled.
    /// </summary>
    private Image healthBar;

    [Tooltip("Optional background image for the health bar.")]
    [SerializeField]
    /// <summary>
    /// Valgfri baggrundsimage til health baren.
    /// </summary>
    private Image healthBarBG;

    /// <summary>
    /// Sidste opdaterede aktuelle liv, brugt til at undgå unødvendige UI-opdateringer.
    /// Initialiseret til en værdi uden for normal rækkevidde.
    /// </summary>
    private int _lastHealth = int.MinValue;

    /// <summary>
    /// Sidste opdaterede max-liv, brugt til at undgå unødvendige UI-opdateringer.
    /// Initialiseret til en værdi uden for normal rækkevidde.
    /// </summary>
    private int _lastMax = int.MinValue;

    /// <summary>
    /// Unity Start-metode. Hvis en Monster-reference allerede er sat, opdateres UI'en og objektet aktiveres.
    /// Hvis ikke, forbliver gameObject inaktivt indtil en Monster sættes.
    /// </summary>
    private void Start()
    {
        if (monster != null)
        {
            UpdateHealthUI(monster.CurrentHealth, monster.MaxHealth);
            gameObject.SetActive(true);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Unity Update-metode. Tjekker hver frame om livsværdierne har ændret sig og opdaterer UI hvis nødvendigt.
    /// Returnerer tidligt hvis UI ikke er aktiv eller der ikke er nogen Monster sat.
    /// </summary>
    private void Update()
    {
        if (!gameObject.activeInHierarchy) return;
        if (monster == null) return;

        if (monster.CurrentHealth != _lastHealth || monster.MaxHealth != _lastMax)
        {
            UpdateHealthUI(monster.CurrentHealth, monster.MaxHealth);
        }
    }
        
    /// <summary>
    /// Opdaterer UI med de givne livsværdier.
    /// </summary>
    /// <param name="current">Aktuelt liv (kan være uden for grænser; metoden sikrer en sikker værdi).</param>
    /// <param name="max">Max liv (kan være 0 eller negativ; metoden sørger for en minimumsværdi på 1).</param>
    /// <remarks>
    /// Metoden sørger for at:
    /// - Max er mindst 1 for at undgå division med nul.
    /// - Current clamps mellem 0 og max.
    /// - Opdaterer både tekst og health bar fill hvis disse references er sat.
    /// - Opdaterer interne _lastHealth og _lastMax værdier.
    /// </remarks>
    public void UpdateHealthUI(int current, int max)
    {
        int safeMax = Mathf.Max(1, max);
        int safeCurrent = Mathf.Clamp(current, 0, safeMax);

        if (healthText != null)
        {
            healthText.text = $"{safeCurrent} / {safeMax}";
        }

        if (healthBar != null)
        {
            healthBar.fillAmount = (float)safeCurrent / safeMax;
        }

        _lastHealth = safeCurrent;
        _lastMax = safeMax;
    }

    /// <summary>
    /// Opdaterer UI ved at bruge den tilknyttede <see cref="monster"/> reference, hvis den eksisterer.
    /// </summary>
    public void UpdateHealthUI()
    {
        if (monster != null)
            UpdateHealthUI(monster.CurrentHealth, monster.MaxHealth);
    }

    /// <summary>
    /// Sætter referencen til et <see cref="Monster"/> og opdaterer UI'ens værdier.
    /// </summary>
    /// <param name="monster">Monsteret som UI'en skal vise liv for.</param>
    public void SetMonster(Monster monster)
    {
        this.monster = monster;
        UpdateHealthUI();
    }

    /// <summary>
    /// Sætter Monster og viser eller skjuler UI'en afhængig af om monster-parameteren er null.
    /// </summary>
    /// <param name="monster">Monsteret der skal vises. Hvis <c>null</c> skjules UI'en.</param>
    public void ShowForMonster(Monster monster)
    {
        SetMonster(monster);
        if (monster != null)
            Show();
        else
            Hide();
    }

    /// <summary>
    /// Gør UI gameObject aktivt og opdaterer visningen fra den tilknyttede Monster (hvis sat).
    /// </summary>
    public void Show()
    {
        gameObject.SetActive(true);
        UpdateHealthUI();
    }

    /// <summary>
    /// Deaktiverer UI gameObject.
    /// </summary>
    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
