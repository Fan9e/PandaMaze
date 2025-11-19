using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI-komponent til at vise monsterets liv.
/// Opdaterer både numerisk tekst og healthbar baseret på en <see cref="Monster"/>-komponent.
/// Hvis <see cref="monster"/> ikke er sat i inspektøren forsøger scriptet at finde en på samme GameObject eller i scenen.
/// </summary>
public class MonsterHealthUI : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The Monster component providing CurrentHealth and MaxHealth. If empty the script will try to find one on the GameObject or in the scene.")]
    [SerializeField] private Monster monster;

    [Tooltip("TextMeshProUGUI used to show numeric health (optional).")]
    [SerializeField] private TextMeshProUGUI healthText;

    [Tooltip("Image used as the health bar fill (Image.type should be Filled).")]
    [SerializeField] private Image healthBar;

    [Tooltip("Optional background image for the health bar.")]
    [SerializeField] private Image healthBarBG;

    private int _lastHealth = int.MinValue;
    private int _lastMax = int.MinValue;

    private void Start()
    {
        if (monster == null)
        {
            monster = GetComponent<Monster>() ?? FindObjectOfType<Monster>();
        }

        if (monster != null)
        {
            UpdateHealthUI(monster.CurrentHealth, monster.MaxHealth);
        }
        else
        {
            UpdateHealthUI(0, 1);
        }
    }

    private void Update()
    {
        if (monster == null)
            return;

        if (monster.CurrentHealth != _lastHealth || monster.MaxHealth != _lastMax)
        {
            UpdateHealthUI(monster.CurrentHealth, monster.MaxHealth);
        }
    }

    /// <summary>
    /// Opdaterer UI til de angivne livsværdier.
    /// Metoden clamper aktuelle værdi til intervallet [0, max] og sørger for at max er mindst 1.
    /// </summary>
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
    /// Opdaterer UI ud fra den tilknyttede <see cref="Monster"/>-komponent.
    /// Gør intet hvis ingen <see cref="monster"/> er tildelt.
    /// </summary>
    public void UpdateHealthUI()
    {
        if (monster != null)
            UpdateHealthUI(monster.CurrentHealth, monster.MaxHealth);
    }

    /// <summary>
    /// Sætter <see cref="monster"/>-referencen og opdaterer UI umiddelbart.
    /// </summary>
    public void SetMonster(Monster monster)
    {
        this.monster = monster;
        UpdateHealthUI();
    }
}
