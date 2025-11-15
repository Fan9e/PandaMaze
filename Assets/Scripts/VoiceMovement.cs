using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
#if UNITY_EDITOR || UNITY_ANDROID || UNITY_IOS
using SpeechToTextNamespace;
#endif

[DisallowMultipleComponent]
public class VoiceMovement : MonoBehaviour
#if UNITY_EDITOR || UNITY_ANDROID || UNITY_IOS
	, ISpeechToTextListener
#endif
{
    [Header("Microphone")]
    public bool isMicrophoneOn = false;
    public bool preferOfflineRecognition = false;
    public bool useFreeFormLanguageModel = true;

    [Header("UI")]
    public Button micToggleButton;
    public TextMeshProUGUI micToggleTMPText;
    public TextMeshProUGUI lastRecognizedTMPText;
    public string micOnText = "Mikrofon: Til";
    public string micOffText = "Mikrofon: Fra";

    void Start()
    {
        if (micToggleButton != null)
        {
            micToggleButton.onClick.RemoveListener(ToggleMicrophone);
            micToggleButton.onClick.AddListener(ToggleMicrophone);
        }

        UpdateMicButtonUI();
        UpdateLastRecognizedUI(string.Empty);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.M))
            ToggleMicrophone();
    }

    #region Public API
    public void ToggleMicrophone()
    {
        if (isMicrophoneOn) StopMicrophone();
        else StartMicrophone();

        UpdateMicButtonUI();
    }

    public void StartMicrophone()
    {
        if (isMicrophoneOn || SpeechToText.IsBusy())
            return;

        SpeechToText.RequestPermissionAsync(permission =>
        {
            if (permission == SpeechToText.Permission.Granted)
            {
                bool started = SpeechToText.Start(this, useFreeFormLanguageModel, preferOfflineRecognition);
                if (started) isMicrophoneOn = true;
                else Debug.LogWarning("VoiceMovement: Kunne ikke starte tale-session");
            }
            else
            {
                Debug.LogWarning("VoiceMovement: Mikrofon / tale-tilladelse ikke givet");
            }

            UpdateMicButtonUI();
        });
    }

    public void StopMicrophone()
    {
        if (!isMicrophoneOn) return;

        SpeechToText.ForceStop();
        isMicrophoneOn = false;
        Debug.Log("VoiceMovement: Mikrofon stoppet");

        UpdateMicButtonUI();
    }
    #endregion

    private void UpdateLastRecognizedUI(string phrase)
    {
        if (lastRecognizedTMPText != null) lastRecognizedTMPText.text = phrase;
    }

    private void UpdateMicButtonUI()
    {
        if (micToggleTMPText != null)
        {
            micToggleTMPText.text = isMicrophoneOn ? micOnText : micOffText;
        }
    }

    #region ISpeechToTextListener implementation
#if UNITY_EDITOR || UNITY_ANDROID || UNITY_IOS
	public void OnReadyForSpeech() => Debug.Log("VoiceMovement: Klar til tale");
	public void OnBeginningOfSpeech() => Debug.Log("VoiceMovement: Tale begyndt");

	public void OnPartialResultReceived(string partial)
	{
		Debug.Log($"VoiceMovement delvist: {partial}");
        UpdateLastRecognizedUI(partial);
	}

	public void OnResultReceived(string spokenText, int? errorCode)
	{
		if (errorCode.HasValue && errorCode.Value != 0)
		{
			Debug.LogWarning($"VoiceMovement: Tale returnerede fejlkode {errorCode.Value}");
            UpdateLastRecognizedUI($"Fejl {errorCode.Value}");
            StopMicrophone();
			return;
		}

		Debug.Log($"VoiceMovement resultat: {spokenText}");
        UpdateLastRecognizedUI(spokenText);

        StopMicrophone();
	}

	public void OnVoiceLevelChanged(float level) { }
#endif
    #endregion
}