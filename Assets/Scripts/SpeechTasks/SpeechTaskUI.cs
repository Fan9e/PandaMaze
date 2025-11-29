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

    public UnityEvent OnTaskCompleted;

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
            // Kan evt. bruges til en lille mic-animering, hvis du vil.
        }

        public void OnMicrophoneStateChanged(bool isOn)
        {
            // Ikke nødvendigt her, men findes i interfacet.
        }
    }

    private TaskObserver taskObserver;

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

    public void StartTask()
    {
        // Bruges af en UI-knap uden parametre
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

        if (normalizedHeard == normalizedTarget)
        {
            feedbackText.text = "Rigtigt! 🐼";
            StopListening();
            OnTaskCompleted?.Invoke();
            panel.SetActive(false);
        }
        else
        {
            feedbackText.text = "Jeg hørte: \"" + recognizedText + "\"\nPrøv igen.";
        }
    }

    private string Normalize(string s)
    {
        if (string.IsNullOrWhiteSpace(s))
            return "";

        s = s.ToLowerInvariant().Trim();
        s = s.Replace(".", "").Replace(",", "").Replace("!", "").Replace("?", "");
        return s;
    }

    private void StartListening()
    {
        if (!Application.isMobilePlatform)
        {
            Debug.Log("SpeechTaskUI: Spring talegenkendelse over i Editor.");
            return;
        }

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