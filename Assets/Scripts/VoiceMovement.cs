using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
#if UNITY_EDITOR || UNITY_ANDROID || UNITY_IOS
using SpeechToTextNamespace;
#endif

/// <summary>
/// Attach to a GameObject to control its movement with voice commands.
/// Supports Danish and English direction words (up/op, down/ned, left/venstre, right/højre).
/// Call ToggleMicrophone() from UI to start/stop speech recognition (or press M in editor/runtime).
/// </summary>
[DisallowMultipleComponent]
public class VoiceMovement : MonoBehaviour
#if UNITY_EDITOR || UNITY_ANDROID || UNITY_IOS
	, ISpeechToTextListener
#endif
{
    [Header("Movement")]
    public Transform target;               // object to move (defaults to this.transform if null)
    public float stepDistance = 1f;        // distance moved per recognized command
    public bool useContinuousMove = false; // if true, a command will set a direction and move each Update

    [Header("Microphone")]
    public bool isMicrophoneOn = false;
    public bool preferOfflineRecognition = false;
    public bool useFreeFormLanguageModel = true;

    [Header("UI (optional)")]
    public Button micToggleButton;                  // optional UI Button to toggle microphone
    public Text micToggleButtonText;                // optional legacy UI Text inside the button
    public TextMeshProUGUI micToggleTMPText;        // optional TextMeshPro text inside the button
    public Sprite micOnSprite;                      // optional icon when mic on
    public Sprite micOffSprite;                     // optional icon when mic off
    public Image micIconImage;                      // optional Image inside the button to show icon
    [Tooltip("Optional UI element to show the last recognized phrase (legacy Text).")]
    public Text lastRecognizedText;
    [Tooltip("Optional TextMeshPro UI element to show the last recognized phrase.")]
    public TextMeshProUGUI lastRecognizedTMPText;
    public string micOnText = "Mic: On";
    public string micOffText = "Mic: Off";

    // internal
    private Vector3 continuousDirection = Vector3.zero;
    private static readonly string[] upWords = { "up", "op", "opad" };
    private static readonly string[] downWords = { "down", "ned", "nedad" };
    private static readonly string[] leftWords = { "left", "venstre" };
    private static readonly string[] rightWords = { "right", "højre", "hojre" };
    private static readonly string[] stopWords = { "stop", "stoppe", "hold", "hold op" };

    void Reset()
    {
        target = transform;
    }

    void Start()
    {
        // subscribe UI button if assigned
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
        // quick keyboard toggle for testing
        if (Input.GetKeyDown(KeyCode.M))
            ToggleMicrophone();

        if (useContinuousMove && continuousDirection != Vector3.zero)
        {
            if (target == null) target = transform;
            target.Translate(continuousDirection * stepDistance * Time.deltaTime, Space.World);
        }
    }

    #region Public API
    public void ToggleMicrophone()
    {
        if (isMicrophoneOn)
            StopMicrophone();
        else
            StartMicrophone();

        UpdateMicButtonUI();
    }

    public void StartMicrophone()
    {
        if (isMicrophoneOn || SpeechToText.IsBusy())
            return;

        // Request permission then start
        SpeechToText.RequestPermissionAsync((permission) =>
        {
            if (permission == SpeechToText.Permission.Granted)
            {
                bool started = SpeechToText.Start(this, useFreeFormLanguageModel, preferOfflineRecognition);
                if (started)
                {
                    isMicrophoneOn = true;
                    Debug.Log("VoiceMovement: Microphone started");
                }
                else
                {
                    Debug.LogWarning("VoiceMovement: Failed to start speech session");
                }
            }
            else
            {
                Debug.LogWarning("VoiceMovement: Microphone / speech permission not granted");
            }

            // update UI (callback from plugin should be on main thread; safe to call)
            UpdateMicButtonUI();
        });
    }

    public void StopMicrophone()
    {
        if (!isMicrophoneOn)
            return;

        SpeechToText.ForceStop();
        isMicrophoneOn = false;
        continuousDirection = Vector3.zero;
        Debug.Log("VoiceMovement: Microphone stopped");

        UpdateMicButtonUI();
    }
    #endregion

    #region Command parsing & movement
    private void ExecuteCommand(Direction cmd)
    {
        if (target == null)
            target = transform;

        switch (cmd)
        {
            case Direction.Up:
                if (useContinuousMove) continuousDirection = Vector3.up;
                else target.Translate(Vector3.up * stepDistance, Space.World);
                break;
            case Direction.Down:
                if (useContinuousMove) continuousDirection = Vector3.down;
                else target.Translate(Vector3.down * stepDistance, Space.World);
                break;
            case Direction.Left:
                if (useContinuousMove) continuousDirection = Vector3.left;
                else target.Translate(Vector3.left * stepDistance, Space.World);
                break;
            case Direction.Right:
                if (useContinuousMove) continuousDirection = Vector3.right;
                else target.Translate(Vector3.right * stepDistance, Space.World);
                break;
            case Direction.Stop:
                continuousDirection = Vector3.zero;
                break;
            default:
                break;
        }
    }

    private Direction GetCommandFromText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return Direction.None;

        // normalize to lower and remove diacritics that commonly appear in speech-to-text
        string normalized = RemoveDiacritics(text).ToLowerInvariant();

        // split into words
        var words = Regex.Split(normalized, @"\W+").Where(w => !string.IsNullOrEmpty(w)).ToArray();
        if (words.Length == 0) return Direction.None;

        // check for stop first
        if (words.Any(w => stopWords.Contains(w))) return Direction.Stop;
        if (words.Any(w => upWords.Contains(w))) return Direction.Up;
        if (words.Any(w => downWords.Contains(w))) return Direction.Down;
        if (words.Any(w => leftWords.Contains(w))) return Direction.Left;
        if (words.Any(w => rightWords.Contains(w))) return Direction.Right;

        // fallback: check if the full phrase contains keywords
        var joined = string.Join(" ", words);
        if (upWords.Any(k => joined.Contains(k))) return Direction.Up;
        if (downWords.Any(k => joined.Contains(k))) return Direction.Down;
        if (leftWords.Any(k => joined.Contains(k))) return Direction.Left;
        if (rightWords.Any(k => joined.Contains(k))) return Direction.Right;

        return Direction.None;
    }

    private static string RemoveDiacritics(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        // quick replacements for Danish letters, keep it small and safe
        text = text.Replace('æ', 'a').Replace('Æ', 'A');
        text = text.Replace('ø', 'o').Replace('Ø', 'O');
        text = text.Replace('å', 'a').Replace('Å', 'A');
        // also replace common composed forms
        text = text.Replace('é', 'e').Replace('É', 'E');
        return text;
    }

    private enum Direction { None, Up, Down, Left, Right, Stop }
    #endregion

    #region UI helpers
    private void UpdateMicButtonUI()
    {
        // text
        if (micToggleButtonText != null)
            micToggleButtonText.text = isMicrophoneOn ? micOnText : micOffText;

        if (micToggleTMPText != null)
            micToggleTMPText.text = isMicrophoneOn ? micOnText : micOffText;

        // icon sprite
        if (micIconImage != null)
        {
            if (micOnSprite != null && micOffSprite != null)
                micIconImage.sprite = isMicrophoneOn ? micOnSprite : micOffSprite;

            // also tint fallback: green/red
            micIconImage.color = isMicrophoneOn ? Color.green : Color.red;
        }
    }

    private void UpdateLastRecognizedUI(string phrase)
    {
        if (lastRecognizedText != null)
            lastRecognizedText.text = phrase;
        if (lastRecognizedTMPText != null)
            lastRecognizedTMPText.text = phrase;
    }
    #endregion

    #region ISpeechToTextListener implementation (platform-specific)
#if UNITY_EDITOR || UNITY_ANDROID || UNITY_IOS
	public void OnReadyForSpeech()
	{
		Debug.Log("VoiceMovement: Ready for speech");
	}

	public void OnBeginningOfSpeech()
	{
		Debug.Log("VoiceMovement: Beginning of speech");
	}

	public void OnPartialResultReceived(string partial)
	{
		// optional: show partial results
		Debug.Log($"VoiceMovement partial: {partial}");
        UpdateLastRecognizedUI(partial);
	}

	// Note: plugin's emulator calls OnResultReceived(spokenText, int? errorCode) with nullable int.
	// Implement with nullable int to match plugin signature.
	public void OnResultReceived(string spokenText, int? errorCode)
	{
		if (errorCode.HasValue && errorCode.Value != 0)
		{
			Debug.LogWarning($"VoiceMovement: Speech returned error code {errorCode.Value}");
            UpdateLastRecognizedUI($"Error {errorCode.Value}");
			return;
		}

		Debug.Log($"VoiceMovement result: {spokenText}");
        UpdateLastRecognizedUI(spokenText);

		var cmd = GetCommandFromText(spokenText);
		if (cmd != Direction.None)
			ExecuteCommand(cmd);
	}

	public void OnVoiceLevelChanged(float level)
	{
		// useful for VU meter or feedback
		// Debug.Log($"Voice level: {level}");
	}
#endif
    #endregion
}