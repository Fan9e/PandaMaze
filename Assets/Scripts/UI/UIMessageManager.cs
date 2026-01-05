using System.Collections;
using UnityEngine;
using TMPro;

public class UIMessageManager : MonoBehaviour
{
    /// <summary>
    /// Singleton-reference til UIMessageManager, så andre scripts kan vise beskeder globalt.
    /// </summary>
    public static UIMessageManager Instance { get; private set; }

    [SerializeField]
    private TMP_Text messageText;

    [SerializeField]
    [Tooltip("Objektet der skal vises/skjules (kan være selve teksten eller et panel).")]
    private GameObject messageRoot;

    private Coroutine _currentRoutine; // En Coroutine gør det muligt at lave ventetid og tidsbaserede handlinger i spillet uden at bruge Update() og uden at pause spillet.

    /// <summary>
    /// Initialiserer UIMessageManager ved at sætte Singleton-instansen
    /// og konfigurere det UI-element, der skal bruges til at vise beskeder.
    /// </summary>
    private void Awake()
    {
        SetupSingleton();
        InitializeMessageRoot();
    }

    /// <summary>
    /// Sikrer, at der kun findes én aktiv instans af UIMessageManager.
    /// Hvis en anden instans allerede eksisterer, bliver denne destrueret.
    /// </summary>
    private void SetupSingleton()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    /// <summary>
    /// Finder og klargør det UI-objekt, der skal vises og skjules,
    /// når der kommer en besked. Starter altid med at skjule elementet.
    /// </summary>
    private void InitializeMessageRoot()
    {
        if (messageRoot == null && messageText != null)
        {
            messageRoot = messageText.gameObject;
        }
        if (messageRoot != null)
        {
            messageRoot.SetActive(false);
        }
    }

    /// <summary>
    /// Viser en tekstbesked på skærmen i et bestemt antal sekunder.
    /// Hvis der allerede kører en tidligere timer, bliver den stoppet.
    /// </summary>
    /// <param name="text">Den tekst, der skal vises for spilleren.</param>
    /// <param name="duration">Hvor længe beskeden skal være synlig (i sekunder).</param>
    public void ShowMessage(string text, float duration)
    {
        if (messageText == null || messageRoot == null)
            return;

        // Stop evt. tidligere timer
        if (_currentRoutine != null)
        {
            StopCoroutine(_currentRoutine);
        }

        messageText.text = text;
        messageRoot.SetActive(true);

        _currentRoutine = StartCoroutine(HideAfterSeconds(duration));
    }

    /// <summary>
    /// Skjuler beskeden efter et antal sekunder.
    /// Bruges af ShowMessage til automatisk at fjerne UI-elementet.
    /// </summary>
    /// <param name="seconds">Tid i sekunder før beskeden skjules.</param>
    /// <returns>En IEnumerator der muliggør ventetiden via coroutine.</returns>
    private IEnumerator HideAfterSeconds(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        messageRoot.SetActive(false);
        _currentRoutine = null;
    }
}
