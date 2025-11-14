using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Globalization;
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
    [Header("Movement")]
    public Transform target;
    public float stepDistance = 1f;
    public bool useContinuousMove = false;

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

    private Vector3 continuousDirection = Vector3.zero;

    public enum Direction { None, Up, Down, Left, Right, Forward, Back }

    [Serializable]
    public struct DirectionKeywords
    {
        public Direction direction;
        [Tooltip("Comma/space-separated phrases that map to this direction")]
        public string[] words;
    }

    [Header("Keywords (order = parse priority)")]
    [Tooltip("Editable list of direction -> keywords. Order defines priority when parsing.")]
    public List<DirectionKeywords> keywordsByDirection = new List<DirectionKeywords>
    {
        new DirectionKeywords { direction = Direction.Up, words = new[] { "up", "op", "opad" } },
        new DirectionKeywords { direction = Direction.Down, words = new[] { "down", "ned", "nedad" } },
        new DirectionKeywords { direction = Direction.Left, words = new[] { "left", "venstre" } },
        new DirectionKeywords { direction = Direction.Right, words = new[] { "right", "højre", "hojre" } },
        new DirectionKeywords { direction = Direction.Forward, words = new[] { "forward", "frem", "fremad", "fremfor" } },
        new DirectionKeywords { direction = Direction.Back, words = new[] { "back", "backwards", "backward", "tilbage", "bagud" } },
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
    private void ExecuteCommand(Direction cmd)
    {
        var t = target ?? transform;
        var dirVec = GetDirectionVector(cmd, t);
        if (dirVec == Vector3.zero) return;

        if (useContinuousMove) continuousDirection = dirVec;
        else t.Translate(dirVec * stepDistance, Space.World);
    }

    private Vector3 GetDirectionVector(Direction dir, Transform t)
    {
        switch (dir)
        {
            case Direction.Up: return Vector3.up;
            case Direction.Down: return Vector3.down;
            case Direction.Left: return Vector3.left;
            case Direction.Right: return Vector3.right;
            case Direction.Forward: return t.forward;
            case Direction.Back: return -t.forward;
            default: return Vector3.zero;
        }
    }

    private Direction GetCommandFromText(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return Direction.None;

        string normalized = RemoveDiacritics(text).ToLowerInvariant();

        var tokens = Regex.Split(normalized, @"\W+").Where(w => !string.IsNullOrEmpty(w)).ToArray();
        if (tokens.Length == 0) return Direction.None;
        var tokenSet = new HashSet<string>(tokens);
        var joined = string.Join(" ", tokens);

        foreach (var entry in keywordsByDirection)
        {
            if (entry.words == null || entry.words.Length == 0) continue;

            if (entry.words.Any(k => tokenSet.Contains(k))) return entry.direction;

            if (entry.words.Any(k => joined.Contains(k))) return entry.direction;
        }

        return Direction.None;
    }

    private static string RemoveDiacritics(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;

        var sb = new StringBuilder(text);
        sb.Replace('æ', 'a').Replace('Æ', 'A');
        sb.Replace('ø', 'o').Replace('Ø', 'O');
        sb.Replace('å', 'a').Replace('Å', 'A');
        sb.Replace('é', 'e').Replace('É', 'E');

        string normalized = sb.ToString().Normalize(NormalizationForm.FormD);
        var clean = new StringBuilder();

        foreach (var ch in normalized)
        {
            var uc = CharUnicodeInfo.GetUnicodeCategory(ch);
            if (uc != System.Globalization.UnicodeCategory.NonSpacingMark)
                clean.Append(ch);
        }

        return clean.ToString().Normalize(NormalizationForm.FormC);
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

		var cmd = GetCommandFromText(spokenText);
		if (cmd != Direction.None) ExecuteCommand(cmd);

        StopMicrophone(clearDirection: false);
	}

	public void OnVoiceLevelChanged(float level) { }
#endif
    #endregion
}