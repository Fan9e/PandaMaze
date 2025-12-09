using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class SpeechTaskUI : MonoBehaviour
{
    [Header("UI references")]
    [SerializeField] private GameObject panel;
    [SerializeField] private TextMeshProUGUI instructionText;
    [SerializeField] private TextMeshProUGUI sentenceText;
    [SerializeField] private TextMeshProUGUI feedbackText;

    [Header("Task settings")]
    [TextArea]
    [SerializeField] private string sentenceToSay = "Jeg har set en pirat";

    [Header("Voice")]
    [SerializeField] private VoiceMovement voiceMovement;

    // Succes (rigtig sætning)
    public UnityEvent OnTaskCompleted;
    // Fejl (forkert sætning)
    public UnityEvent OnTaskFailed;

    private string normalizedTarget;

    private class TaskObserver : IVoiceObserver
    {
        private readonly SpeechTaskUI owner;

        public TaskObserver(SpeechTaskUI owner)
        {
            this.owner = owner;
        }

        public void OnPartialResult(string partial)
        {
            if (owner.feedbackText != null)
                owner.feedbackText.text = partial;
        }

        public void OnResult(string result)
        {
            owner.OnSpeechRecognized(result);
        }

        public void OnVoiceLevelChanged(float level)
        {
        }

        public void OnMicrophoneStateChanged(bool isOn)
        {
        }
    }

    private TaskObserver taskObserver;

    // Bruges af kamp-scriptet til at tjekke om opgaven er åben
    public bool IsActive => panel != null && panel.activeSelf;

    private void Awake()
    {
        if (panel == null) panel = gameObject;

        if (instructionText != null)
            instructionText.text = "Sig denne sætning:";

        SetupSentence(sentenceToSay);
        panel.SetActive(false);
    }

    /// <summary>
    /// Almindelig opgave: vis sætningen som den er, og bed spilleren gentage den.
    /// </summary>
    public void ShowTask(string newSentence = null)
    {
        if (!string.IsNullOrEmpty(newSentence))
        {
            SetupSentence(newSentence);
        }

        if (instructionText != null)
            instructionText.text = "Sig denne sætning:";

        if (feedbackText != null)
            feedbackText.text = "";

        panel.SetActive(true);
        StartListening();
    }

    /// <summary>
    /// Opgave hvor ordene i sætningen er blandet, og spilleren skal sige sætningen korrekt.
    /// </summary>
    public void ShowScrambledTask(string correctSentence)
    {
        if (string.IsNullOrWhiteSpace(correctSentence))
        {
            // Fallback til normal opgave, hvis der kom noget mærkeligt ind.
            ShowTask();
            return;
        }

        // Brug den korrekte sætning som "target" for sammenligning
        SetupSentence(correctSentence);

        // Bland ordene til visning
        string scrambled = ScrambleWords(correctSentence);

        if (instructionText != null)
            instructionText.text = "Sæt ordene i rigtig rækkefølge og sig sætningen:";

        if (sentenceText != null)
            sentenceText.text = scrambled;

        if (feedbackText != null)
            feedbackText.text = "";

        panel.SetActive(true);
        StartListening();
    }

    /// <summary>
    /// Kan stadig bruges af en knap, hvis du vil.
    /// </summary>
    public void StartTask()
    {
        ShowTask();
    }

    private void SetupSentence(string text)
    {
        sentenceToSay = text;

        if (sentenceText != null)
            sentenceText.text = "\"" + sentenceToSay + "\"";

        normalizedTarget = Normalize(sentenceToSay);
    }

    public void OnSpeechRecognized(string recognizedText)
    {
        string normalizedHeard = Normalize(recognizedText);

        // Ét forsøg pr. runde – vi stopper altid mikrofonen her
        StopListening();

        if (normalizedHeard == normalizedTarget)
        {
            if (feedbackText != null)
                feedbackText.text = "Rigtigt! 🐼";

            OnTaskCompleted?.Invoke();
            // VIGTIGT: vi skjuler IKKE panelet her,
            // så kamp-scriptet kan vise næste opgave på samme panel.
        }
        else
        {
            if (feedbackText != null)
                feedbackText.text = "Jeg hørte: \"" + recognizedText + "\"";

            OnTaskFailed?.Invoke();
            // Panelet bliver stående, så man kan se hvad der blev hørt.
        }
    }

    private string Normalize(string s)
    {
        if (string.IsNullOrWhiteSpace(s))
            return "";

        s = s.ToLowerInvariant().Trim();
        s = s.Replace(".", "")
             .Replace(",", "")
             .Replace("!", "")
             .Replace("?", "");
        return s;
    }

    /// <summary>
    /// Blander rækkefølgen af ordene i en sætning.
    /// </summary>
    private string ScrambleWords(string sentence)
    {
        if (string.IsNullOrWhiteSpace(sentence))
            return sentence;

        string[] words = sentence.Split(' ');

        if (words.Length <= 1)
            return sentence;

        // Fisher-Yates shuffle
        for (int i = 0; i < words.Length; i++)
        {
            int rand = Random.Range(0, words.Length);
            (words[i], words[rand]) = (words[rand], words[i]);
        }

        return string.Join(" ", words);
    }

    private void StartListening()
    {
#if UNITY_EDITOR
        Debug.Log("SpeechTaskUI (Editor): simulerer FORKERT sætning: hello world");
        OnSpeechRecognized("hello world");   // altid forkert i Editor
        return;
#endif

        if (voiceMovement == null)
            voiceMovement = FindObjectOfType<VoiceMovement>();

        if (voiceMovement == null)
        {
            Debug.LogWarning("SpeechTaskUI: Kunne ikke finde VoiceMovement i scenen.");
            return;
        }

        if (taskObserver == null)
            taskObserver = new TaskObserver(this);

        // Kun registrere observer
        voiceMovement.RegisterObserver(taskObserver);

        // Men lad være med at starte mikrofonen automatisk
        if (!voiceMovement.isMicrophoneOn && feedbackText != null)
        {
            feedbackText.text = "Tænd mikrofonen (Mic-knappen) for at sige sætningen.";
        }
    }

    private void StopListening()
    {
        if (voiceMovement == null)
            return;

        if (taskObserver != null)
            voiceMovement.UnregisterObserver(taskObserver);

        voiceMovement.StopMicrophone();
    }
}
