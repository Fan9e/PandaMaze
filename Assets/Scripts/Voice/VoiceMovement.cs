using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
#if UNITY_EDITOR || UNITY_ANDROID || UNITY_IOS
using SpeechToTextNamespace;
#endif

[DisallowMultipleComponent]
/// <summary>
/// Styrer mikrofon / talegenkendelse og spreder resultater til registrerede observatører.
/// Håndterer UI-opdatering for mikrofon-knap og sidste genkendte tekst.
/// </summary>
public class VoiceMovement : MonoBehaviour
#if UNITY_EDITOR || UNITY_ANDROID || UNITY_IOS
	, ISpeechToTextListener
#endif
{
    [Header("Microphone")]
    /// <summary>Om mikrofonen i øjeblikket er tændt.</summary>
    public bool isMicrophoneOn = false;
    /// <summary>Foretræk offline genkendelse, hvis tilgængelig.</summary>
    public bool preferOfflineRecognition = false;
    /// <summary>Brug et frit tekstsprog-model til genkendelse (i stedet for begrænsede grammatikker).</summary>
    public bool useFreeFormLanguageModel = true;

    [Header("UI")]
    /// <summary>Reference til knappen der skifter mikrofon til/fra.</summary>
    public Button micToggleButton;
    /// <summary>Reference til TextMeshPro-teksten der viser mikrofonens tilstand.</summary>
    public TextMeshProUGUI micToggleTMPText;
    /// <summary>Reference til TextMeshPro-teksten der viser sidste genkendte sætning eller fejl.</summary>
    public TextMeshProUGUI lastRecognizedTMPText;
    /// <summary>Tekst der vises når mikrofon er tændt.</summary>
    public string micOnText = "Mikrofon: Til";
    /// <summary>Tekst der vises når mikrofon er slukket.</summary>
    public string micOffText = "Mikrofon: Fra";

    private readonly List<IVoiceObserver> observers = new List<IVoiceObserver>();
    private readonly object observersLock = new object();

    /// <summary>
    /// Initialiserer UI og knap-lister.
    /// </summary>
    private void Start()
    {
        if (micToggleButton != null)
        {
            micToggleButton.onClick.RemoveListener(ToggleMicrophone);
            micToggleButton.onClick.AddListener(ToggleMicrophone);
        }

        UpdateMicButtonUI();
        UpdateLastRecognizedUI(string.Empty);
    }

    /// <summary>
    /// Lytter efter genvejstast (M) for at skifte mikrofonen.
    /// </summary>
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.M))
            ToggleMicrophone();
    }

    #region Observer API
    /// <summary>
    /// Registrerer en observer der modtager delvise/resultat-opdateringer og mikrofontilstand.
    /// </summary>
    /// <param name="observer">Observeren der skal registreres.</param>
    public void RegisterObserver(IVoiceObserver observer)
    {
        if (observer == null) return;
        lock (observersLock)
        {
            if (!observers.Contains(observer))
                observers.Add(observer);
        }
    }

    /// <summary>
    /// Fjern-registrerer en tidligere registreret observer.
    /// </summary>
    /// <param name="observer">Observeren der skal fjernes.</param>
    public void UnregisterObserver(IVoiceObserver observer)
    {
        if (observer == null) return;
        lock (observersLock)
        {
            observers.Remove(observer);
        }
    }

    /// <summary>
    /// Underret alle registrerede observatører om et delvist genkendt resultat.
    /// </summary>
    /// <param name="partial">Den delvise tekst der er genkendt indtil videre.</param>
    private void NotifyPartial(string partial)
    {
        lock (observersLock)
        {
            foreach (var o in observers)
                try 
                { 
                    o.OnPartialResult(partial); 
                } 
                catch (Exception ex) 
                { 
                    Debug.LogWarning($"VoiceMovement: observer threw in OnPartialResult: {ex}"); 
                }
        }
    }

    /// <summary>
    /// Underret alle registrerede observatører om et fuldt genkendt resultat.
    /// </summary>
    /// <param name="result">Den fuldt genkendte tekst (eller fejlbesked).</param>
    private void NotifyResult(string result)
    {
        lock (observersLock)
        {
            foreach (var o in observers)
                try 
                { 
                    o.OnResult(result); 
                } 
                catch (Exception ex) 
                { 
                    Debug.LogWarning($"VoiceMovement: observer threw in OnResult: {ex}"); 
                }
        }
    }

    /// <summary>
    /// Underret alle registrerede observatører om ændring i mikrofon-lydniveau.
    /// </summary>
    /// <param name="level">Lydniveauet (typisk i [0..1] eller platformspecifikt interval).</param>
    private void NotifyVoiceLevel(float level)
    {
        lock (observersLock)
        {
            foreach (var o in observers)
                try 
                { 
                    o.OnVoiceLevelChanged(level); 
                } 
                catch (Exception ex) 
                { 
                    Debug.LogWarning($"VoiceMovement: observer threw in OnVoiceLevelChanged: {ex}"); 
                }
        }
    }

    /// <summary>
    /// Underret alle registrerede observatører om mikrofonens tilstand (tændt/slukket).
    /// </summary>
    /// <param name="isOn">True hvis mikrofon er tændt.</param>
    private void NotifyMicState(bool isOn)
    {
        lock (observersLock)
        {
            foreach (var o in observers)
                try 
                { 
                    o.OnMicrophoneStateChanged(isOn); 
                } 
                catch (Exception ex) 
                { 
                    Debug.LogWarning($"VoiceMovement: observer threw in OnMicrophoneStateChanged: {ex}"); 
                }
        }
    }
    #endregion

    #region Public API
    /// <summary>
    /// Skifter mikrofonens tilstand: starter hvis slukket, stopper hvis tændt.
    /// Opdaterer UI efter handlingen.
    /// </summary>
    public void ToggleMicrophone()
    {
        if (isMicrophoneOn) StopMicrophone();
        else StartMicrophone();

        UpdateMicButtonUI();
    }

    /// <summary>
    /// Forsøger at starte talegenkendelse efter at have anmodet om nødvendige tilladelser.
    /// Hvis start lykkes sættes <see cref="isMicrophoneOn"/> til true og observatører underrettes.
    /// </summary>
    public void StartMicrophone()
    {
        if (isMicrophoneOn || SpeechToText.IsBusy())
            return;

        SpeechToText.RequestPermissionAsync(permission =>
        {
            if (permission == SpeechToText.Permission.Granted)
            {
                bool started = SpeechToText.Start(this, useFreeFormLanguageModel, preferOfflineRecognition);
                if (started)
                {
                    isMicrophoneOn = true;
                    NotifyMicState(true);
                }
                else Debug.LogWarning("VoiceMovement: Could not start speech session");
            }
            else
            {
                Debug.LogWarning("VoiceMovement: Microphone/speech permission not granted");
            }

            UpdateMicButtonUI();
        });
    }

    /// <summary>
    /// Stopper talegenkendelse og opdaterer tilstand + UI.
    /// </summary>
    public void StopMicrophone()
    {
        if (!isMicrophoneOn) return;

        SpeechToText.ForceStop();
        isMicrophoneOn = false;
        Debug.Log("VoiceMovement: Microphone stopped");

        UpdateMicButtonUI();
        NotifyMicState(false);
    }
    #endregion

    /// <summary>
    /// Opdaterer UI-teksten der viser den sidst genkendte sætning eller fejl.
    /// </summary>
    /// <param name="phrase">Teksten der skal vises; tom streng fjerner visning.</param>
    private void UpdateLastRecognizedUI(string phrase)
    {
        if (lastRecognizedTMPText != null) lastRecognizedTMPText.text = phrase;
    }

    /// <summary>
    /// Opdaterer mikrofon-knap-teksten ud fra aktuel mikrofon-tilstand.
    /// </summary>
    private void UpdateMicButtonUI()
    {
        if (micToggleTMPText != null)
        {
            micToggleTMPText.text = isMicrophoneOn ? micOnText : micOffText;
        }
    }

    // Disse metoder implementerer `ISpeechToTextListener`
    // og bruger den platformspecifikke `SpeechToText`-API.
    // `#if UNITY_EDITOR || UNITY_ANDROID || UNITY_IOS` sørger for,
    // at koden kun kompileres på Editor, Android og iOS,
    // så bygninger for andre platforme (fx Standalone eller WebGL)
    // undgår compile-fejl pga. manglende API/namespace.
    // Hvis koden kompileres på en platform uden de definerede muligheder,
    // ville denne kode block ekskluderes,
    // da Visual studio ikke er en af mulighederne forkommer koden derfor grå.
    #region ISpeechToTextListener implementation
#if UNITY_EDITOR || UNITY_ANDROID || UNITY_IOS
    /// <summary>Kaldes når talegenkendelsen er klar til at modtage tale.</summary>
	public void OnReadyForSpeech() => Debug.Log("VoiceMovement: Ready for speech");
	/// <summary>Kaldes når bruger begynder at tale.</summary>
	public void OnBeginningOfSpeech() => Debug.Log("VoiceMovement: Speech started");

	/// <summary>
	/// Modtager delvise mellemliggende genkendelsesresultater.
	/// </summary>
	/// <param name="partial">Den delvise genkendte tekst.</param>
	public void OnPartialResultReceived(string partial)
	{
		Debug.Log($"VoiceMovement partial: {partial}");
        UpdateLastRecognizedUI(partial);
        NotifyPartial(partial);
	}

	/// <summary>
	/// Modtager endeligt resultat eller fejl fra talegenkendelsen.
	/// </summary>
	/// <param name="spokenText">Den genkendte tekst (kan være fejltekst hvis errorCode != 0).</param>
	/// <param name="errorCode">Valgfri fejlkode; null eller 0 betyder succes.</param>
	public void OnResultReceived(string spokenText, int? errorCode)
	{
		if (errorCode.HasValue && errorCode.Value != 0)
		{
			Debug.LogWarning($"VoiceMovement: Speech returned error code {errorCode.Value}");
            var errorText = $"Error {errorCode.Value}";
            UpdateLastRecognizedUI(errorText);
            NotifyResult(errorText);
            StopMicrophone();
			return;
		}

		Debug.Log($"VoiceMovement result: {spokenText}");
        UpdateLastRecognizedUI(spokenText);
        NotifyResult(spokenText);

        StopMicrophone();
	}

	/// <summary>
	/// Kaldes løbende med ændringer i mikrofonens lydniveau.
	/// </summary>
	/// <param name="level">Aktuelt lydniveau.</param>
	public void OnVoiceLevelChanged(float level)
    {
        NotifyVoiceLevel(level);
    }
#endif
    #endregion
}