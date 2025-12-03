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
        instructionText.text = "Sig denne sætning:";
        SetupSentence(sentenceToSay);
        panel.SetActive(false);
    }

    public void ShowTask(string newSentence = null)
    {
        if (!string.IsNullOrEmpty(newSentence))
        {
            SetupSentence(newSentence);
        }

        feedbackText.text = "";
        panel.SetActive(true);
        StartListening();
    }

    // Kan stadig bruges af en knap, hvis du vil,
    // men er ikke nødvendig mere når kampen starter automatisk
    public void StartTask()
    {
        ShowTask();
    }

    private void SetupSentence(string text)
    {
        sentenceToSay = text;
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
            feedbackText.text = "Rigtigt! 🐼";
            OnTaskCompleted?.Invoke();
            panel.SetActive(false);
        }
        else
        {
            feedbackText.text = "Jeg hørte: \"" + recognizedText + "\"";
            OnTaskFailed?.Invoke();
            // Panelet bliver stående, så man kan se hvad der blev hørt.
            // Næste forsøg startes af kamp-scriptet.
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

        voiceMovement.RegisterObserver(taskObserver);
        voiceMovement.StartMicrophone();
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
