using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// UI-komponent der viser en liste af nøgleord for forhindringer og kan automatisk skjule panelet efter en given varighed.
/// </summary>
public class ObstacleKeywordsUI : MonoBehaviour
{
    /// <summary>
    /// Globalt tilgængelig singleton-instans af <see cref="ObstacleKeywordsUI"/>.
    /// </summary>
    public static ObstacleKeywordsUI Instance { get; private set; }

    /// <summary>
    /// Rodobjektet for UI-panelet der vises/skjules.
    /// </summary>
    [SerializeField]
    private GameObject panelRoot;

    /// <summary>
    /// Tekstkomponenten hvor nøgleordene bliver vist.
    /// </summary>
    [SerializeField]
    private TMP_Text keywordsText;

    /// <summary>
    /// Redigerbare lister med nøgleord som kan sættes i inspektøren.
    /// </summary>
    [SerializeField]
    private string[] rockKeywords;

    [SerializeField]
    private string[] bambooKeywords;

    /// <summary>
    /// Reference til den aktive auto-hide coroutine, hvis en sådan kører.
    /// </summary>
    private Coroutine _autoHideRoutine;

    /// <summary>
    /// Hardkodet fallback-liste med nøgleord for klippe-forhindringer.
    /// Bruges når den redigerbare <c>rockKeywords</c>-listen er tom eller ikke initialiseret.
    /// </summary>
    private static readonly string[] DefaultRockKeywords = new[] { "hop" };

    /// <summary>
    /// Hardkodet fallback-liste med nøgleord for bambus-forhindringer.
    /// Bruges når den redigerbare <c>bambooKeywords</c>-listen er tom eller ikke initialiseret.
    /// </summary>
    private static readonly string[] DefaultBambooKeywords = new[] { "duk" };

    /// <summary>
    /// Unity Awake-lifecycle. Sørger for singleton-opførsel, initierer standardreferencer og sikrer at panelet er skjult ved opstart.
    /// </summary>
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (panelRoot == null && keywordsText != null)
            panelRoot = keywordsText.gameObject;

        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    /// <summary>
    /// Viser en kommasepareret liste af nøgleord i UI'et.
    /// </summary>
    /// <param name="keywords">Array af nøgleord. Tomme eller null-strenge bliver fjernet.</param>
    /// <param name="duration">
    /// Hvor lang tid panelet skal være synligt i sekunder. Hvis værdien er 0 eller mindre bliver panelet ikke automatisk skjult.
    /// Standard er 0 (forbliver synligt indtil <see cref="HideKeywords"/> kaldes).
    /// </param>
    /// <remarks>
    /// Metoden sætter teksten til formen "Sig ét af følgende ord: &lt;ordnet liste&gt;".
    /// Hvis der allerede kører en auto-hide coroutine stoppes denne før eventuelt at starte en ny.
    /// </remarks>
    public void ShowKeywords(string[] keywords, float duration = 0f)
    {
        if (keywordsText == null || panelRoot == null)
        {
            Debug.LogWarning("ObstacleKeywordsUI: UI references not set.");
            return;
        }

        string[] cleaned = System.Array.FindAll(keywords ?? new string[0], keyword => !string.IsNullOrWhiteSpace(keyword));
        string joined = cleaned.Length > 0 ? string.Join(", ", cleaned) : "Genstart spillet";

        if (cleaned.Length == 0)
            keywordsText.text = $"Fejl på spillet: {joined}";
        else if (joined.Length > 0)
            keywordsText.text = $"Sig ét af følgende ord: {joined}";

        panelRoot.SetActive(true);

        if (_autoHideRoutine != null)
        {
            StopCoroutine(_autoHideRoutine);
            _autoHideRoutine = null;
        }

        if (duration > 0f)
            _autoHideRoutine = StartCoroutine(AutoHideAfterSeconds(duration));
    }

    /// <summary>
    /// Skjuler nøgleords-panelet og stopper eventuel aktiv auto-hide coroutine.
    /// </summary>
    public void HideKeywords()
    {
        if (panelRoot == null) return;

        if (_autoHideRoutine != null)
        {
            StopCoroutine(_autoHideRoutine);
            _autoHideRoutine = null;
        }

        panelRoot.SetActive(false);
    }

    /// <summary>
    /// Coroutine der venter et antal sekunder og derefter skjuler panelet.
    /// </summary>
    /// <param name="seconds">Antal sekunder der skal ventes før panelet skjules.</param>
    private IEnumerator AutoHideAfterSeconds(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        panelRoot.SetActive(false);
        _autoHideRoutine = null;
    }
}