using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// Viser midlertidige beskeder på skærmen.
/// </summary>
public class UIMessageManager : MonoBehaviour
{
    public static UIMessageManager Instance { get; private set; }

    [SerializeField]
    private TMP_Text messageText;

    [SerializeField]
    [Tooltip("Objektet der skal vises/skjules (kan være selve teksten eller et panel).")]
    private GameObject messageRoot;

    private Coroutine _currentRoutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (messageRoot == null && messageText != null)
        {
            messageRoot = messageText.gameObject;
        }

        if (messageRoot != null)
        {
            messageRoot.SetActive(false); // start skjult
        }
    }

    /// <summary>
    /// Vis en besked i et antal sekunder.
    /// </summary>
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

    private IEnumerator HideAfterSeconds(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        messageRoot.SetActive(false);
        _currentRoutine = null;
    }

    //public void ShowMessage(string text, float duration)
    //{
    //    Debug.Log("ShowMessage bliver kaldt med tekst: " + text);

    //    if (messageText == null || messageRoot == null)
    //    {
    //        Debug.LogWarning("UIMessageManager mangler references!");
    //        return;
    //    }

    //    if (_currentRoutine != null)
    //    {
    //        StopCoroutine(_currentRoutine);
    //    }

    //    messageText.text = text;
    //    messageRoot.SetActive(true);   // <- tænder objektet

    //    _currentRoutine = StartCoroutine(HideAfterSeconds(duration));
    //}
}
