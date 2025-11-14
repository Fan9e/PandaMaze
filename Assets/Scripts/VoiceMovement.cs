using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections.Generic;
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
    [Header("Bevægelse")]
    public Transform target;
    public float stepDistance = 1f;
    public bool useContinuousMove = false;

    [Header("Mikrofon")]
    public bool isMicrophoneOn = false;
    public bool preferOfflineRecognition = false;
    public bool useFreeFormLanguageModel = true;

    [Header("UI (valgfri)")]
    public Button micToggleButton;
    public Text micToggleButtonText;
    public TextMeshProUGUI micToggleTMPText;
    public Sprite micOnSprite;
    public Sprite micOffSprite;
    public Image micIconImage;
    [Tooltip("Valgfrit UI-element til at vise sidst genkendte sætning (legacy Text).")]
    public Text lastRecognizedText;
    [Tooltip("Valgfrit TextMeshPro UI-element til at vise sidst genkendte sætning.")]
    public TextMeshProUGUI lastRecognizedTMPText;
    public string micOnText = "Mikrofon: Til";
    public string micOffText = "Mikrofon: Fra";

    private Vector3 continuousDirection = Vector3.zero;

    private static readonly string[] upWords = { "up", "op", "opad" };
    private static readonly string[] downWords = { "down", "ned", "nedad" };
    private static readonly string[] leftWords = { "left", "venstre" };
    private static readonly string[] rightWords = { "right", "højre", "hojre" };
    private static readonly string[] forwardWords = { "forward", "frem", "fremad", "fremfor" };
    private static readonly string[] backWords = { "back", "backwards", "backward", "tilbage", "bagud" };

    private static readonly (Direction dir, string[] words)[] prioritized =
    {
        (Direction.Up, upWords),
        (Direction.Down, downWords),
        (Direction.Left, leftWords),
        (Direction.Right, rightWords),
        (Direction.Forward, forwardWords),
        (Direction.Back, backWords),
    };

    void Reset()
    {
        target = transform;
    }

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

        if (useContinuousMove && continuousDirection != Vector3.zero)
        {
            var t = target ?? transform;
            t.Translate(continuousDirection * stepDistance * Time.deltaTime, Space.World);
        }
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

        SpeechToText.RequestPermissionAsync((permission) =>
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

    public void StopMicrophone(bool clearDirection = true)
    {
        if (!isMicrophoneOn) return;

        SpeechToText.ForceStop();
        isMicrophoneOn = false;
        if (clearDirection) continuousDirection = Vector3.zero;
        Debug.Log("VoiceMovement: Mikrofon stoppet");

        UpdateMicButtonUI();
    }
    #endregion

    #region Command parsing & movement
    private enum Direction { None, Up, Down, Left, Right, Forward, Back }

    private void ExecuteCommand(Direction cmd)
    {
        var t = target ?? transform;

        switch (cmd)
        {
            case Direction.Up: ApplyMovement(Vector3.up, t); break;
            case Direction.Down: ApplyMovement(Vector3.down, t); break;
            case Direction.Left: ApplyMovement(Vector3.left, t); break;
            case Direction.Right: ApplyMovement(Vector3.right, t); break;
            case Direction.Forward: ApplyMovement(t.forward, t); break;
            case Direction.Back: ApplyMovement(-t.forward, t); break;
            default: break;
        }
    }

    private void ApplyMovement(Vector3 direction, Transform t)
    {
        if (useContinuousMove) continuousDirection = direction;
        else t.Translate(direction * stepDistance, Space.World);
    }

    private Direction GetCommandFromText(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return Direction.None;

        string normalized = RemoveDiacritics(text).ToLowerInvariant();

        var tokens = Regex.Split(normalized, @"\W+").Where(w => !string.IsNullOrEmpty(w)).ToArray();
        if (tokens.Length == 0) return Direction.None;
        var tokenSet = new HashSet<string>(tokens);
        var joined = string.Join(" ", tokens);

        foreach (var entry in prioritized)
        {
            if (entry.words.Any(w => tokenSet.Contains(w))) return entry.dir;
            if (entry.words.Any(w => joined.Contains(w))) return entry.dir;
        }

        return Direction.None;
    }

    private static string RemoveDiacritics(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        return text
            .Replace('æ', 'a').Replace('Æ', 'A')
            .Replace('ø', 'o').Replace('Ø', 'O')
            .Replace('å', 'a').Replace('Å', 'A')
            .Replace('é', 'e').Replace('É', 'E');
    }
    #endregion

    #region UI helpers
    private void UpdateMicButtonUI()
    {
        string micText = isMicrophoneOn ? micOnText : micOffText;
        if (micToggleButtonText != null) micToggleButtonText.text = micText;
        if (micToggleTMPText != null) micToggleTMPText.text = micText;

        if (micIconImage != null)
        {
            if (micOnSprite != null && micOffSprite != null) micIconImage.sprite = isMicrophoneOn ? micOnSprite : micOffSprite;
            micIconImage.color = isMicrophoneOn ? Color.green : Color.red;
        }
    }

    private void UpdateLastRecognizedUI(string phrase)
    {
        if (lastRecognizedText != null) lastRecognizedText.text = phrase;
        if (lastRecognizedTMPText != null) lastRecognizedTMPText.text = phrase;
    }
    #endregion

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

		var cmd = GetCommandFromText(spokenText);
		if (cmd != Direction.None) ExecuteCommand(cmd);

        StopMicrophone(clearDirection: false);
	}

	public void OnVoiceLevelChanged(float level) { }
#endif
    #endregion
}