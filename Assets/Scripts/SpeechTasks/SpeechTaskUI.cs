using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class SpeechTaskUI : MonoBehaviour // hovedklasse
{
    private const int MaximumScrambleAttempts = 10;

    private enum TaskDisplayMode
    {
        Normal,
        ScrambledAllWords,
        ScrambledSingleWord
    }

    private class TaskObserver : IVoiceObserver // indvendig (nested) klasse
    {
        private readonly SpeechTaskUI owner;

        public TaskObserver(SpeechTaskUI owner)
        {
            this.owner = owner;
        }

        /// <summary>
        /// Kaldes løbende mens spilleren taler. Viser delvise resultater
        /// (ord der genkendes undervejs) i feedback-teksten.
        /// </summary>
        public void OnPartialResult(string partial)
        {
            if (owner.feedbackText != null)
                owner.feedbackText.text = partial;
        }

        /// <summary>
        /// Kaldes, når talesystemet mener, at spilleren er færdig med at tale,
        /// og sender den endelige sætning tilbage. Giver resultatet videre til UI’et.
        /// </summary>
        public void OnResult(string result)
        {
            owner.OnSpeechRecognized(result);
        }

        /// <summary>
        /// Kaldes når mikrofonens lydniveau ændrer sig. Bruges ikke her,
        /// men kræves af IVoiceObserver-interfacet.
        /// </summary>
        public void OnVoiceLevelChanged(float level) { }

        /// <summary>
        /// Kaldes når mikrofonen bliver tændt eller slukket. Bruges ikke her,
        /// men kræves af IVoiceObserver-interfacet.
        /// </summary>
        public void OnMicrophoneStateChanged(bool isOn) { }
    }

    [Header("UI-referencer")]
    [SerializeField] private GameObject panel;
    [SerializeField] private TextMeshProUGUI instructionText;
    [SerializeField] private TextMeshProUGUI sentenceText;
    [SerializeField] private TextMeshProUGUI feedbackText;

    [Header("Standardindstillinger")]
    [TextArea]
    [SerializeField] private string defaultSentence = "Jeg har set en pirat";

    [Header("Voice")]
    [SerializeField] private VoiceMovement voiceMovement;

    [Tooltip("Kaldes når barnet har sagt sætningen korrekt.")]
    public UnityEvent OnTaskCompleted;

    [Tooltip("Kaldes når barnet har sagt noget forkert.")]
    public UnityEvent OnTaskFailed;

    private string _targetSentenceRaw;
    private string _targetSentenceNormalized;

    private TaskObserver _taskObserver;

    // Bruges af kamp-scripts til at se om panelet er synligt
    public bool IsActive => panel != null && panel.activeSelf;


    /// <summary>
    /// Initialiserer UI-elementerne, sætter standardtekster,
    /// og skjuler panelet fra start.
    /// </summary>
    private void Awake()
    {
        if (panel == null)
            panel = gameObject;

        InitializeDefaultTexts();
        SetupSentence(defaultSentence);

        panel.SetActive(false);
    }

    /// <summary>
    /// Sætter standard-tekster for instruktion og feedback,
    /// så der altid vises noget forståeligt i UI’et.
    /// </summary>
    private void InitializeDefaultTexts()
    {
        if (instructionText != null)
            instructionText.text = "Sig denne sætning:";

        if (feedbackText != null)
            feedbackText.text = "";
    }

    /// <summary>
    /// Viser en normal tale-opgave, hvor barnet skal gentage sætningen præcist.
    /// </summary>
    public void ShowTask(string sentence = null)
    {
        ShowTaskInternal(sentence, TaskDisplayMode.Normal);
    }

    /// <summary>
    /// Viser en opgave, hvor alle ordene i sætningen er blandet tilfældigt,
    /// og barnet skal sige den korrekte rækkefølge.
    /// </summary>
    public void ShowScrambledTask(string correctSentence)
    {
        ShowTaskInternal(correctSentence, TaskDisplayMode.ScrambledAllWords);
    }

    /// <summary>
    /// Opgave hvor præcis ét ord står forkert.
    /// </summary>
    public void ShowOneWordScrambledTask(string correctSentence)
    {
        ShowTaskInternal(correctSentence, TaskDisplayMode.ScrambledSingleWord);
    }

    /// <summary>
    /// Kan stadig bruges af en UI-knap.
    /// </summary>
    public void StartTask()
    {
        ShowTask();
    }

    /// <summary>
    /// Fælles metode som alle opgavetyper bruger.
    /// Forbereder sætningen, vælger visningsform (normal/scrambled),
    /// opdaterer UI og starter mikrofonlytning.
    /// </summary>
    private void ShowTaskInternal(string sentence, TaskDisplayMode mode)
    {
        // Hvis kamp-scriptet ikke sender noget, brug default
        if (string.IsNullOrWhiteSpace(sentence))
            sentence = defaultSentence;

        // Gem og normalisér targetsætning
        SetupSentence(sentence);

        // Vælg hvordan sætningen skal vises
        string displaySentence = GetDisplaySentence(sentence, mode);
        string instruction = GetInstructionTextForMode(mode);

        UpdateUIForNewTask(instruction, displaySentence);

        panel.SetActive(true);
        StartListening();
    }

    /// <summary>
    /// Gemmer den oprindelige sætning og laver en normaliseret version
    /// (små bogstaver, ingen tegnsætning), så vi kan sammenligne præcist
    /// med barnets svar.
    /// </summary>
    private void SetupSentence(string text)
    {
        _targetSentenceRaw = text ?? "";
        _targetSentenceNormalized = Normalize(_targetSentenceRaw);
    }

    /// <summary>
    /// Returnerer den version af sætningen som skal vises på UI’et,
    /// baseret på opgavetypen (normal, scrambled, eller one-word scrambled).
    /// </summary>
    private string GetDisplaySentence(string sentence, TaskDisplayMode mode)
    {
        switch (mode)
        {
            case TaskDisplayMode.ScrambledAllWords:
                return ScrambleWords(sentence);

            case TaskDisplayMode.ScrambledSingleWord:
                return ScrambleOneWord(sentence);

            case TaskDisplayMode.Normal:
            default:
                // Normalopgave: vis med citationstegn
                return $"\"{sentence}\"";
        }
    }

    /// <summary>
    /// Returnerer den tekst, der skal stå som instruktion for barnet,
    /// afhængigt af hvilken type opgave barnet får.
    /// </summary>
    private string GetInstructionTextForMode(TaskDisplayMode mode)
    {
        switch (mode)
        {
            case TaskDisplayMode.ScrambledAllWords:
                return "Sæt ordene i rigtig rækkefølge og sig sætningen:";

            case TaskDisplayMode.ScrambledSingleWord:
                return "Ét ord står forkert. Sig sætningen korrekt:";

            case TaskDisplayMode.Normal:
            default:
                return "Sig denne sætning:";
        }
    }

    /// <summary>
    /// Opdaterer UI-text fields med instruktionen og den visuelle opgave,
    /// og nulstiller feedback-området.
    /// </summary>
    private void UpdateUIForNewTask(string instruction, string displaySentence)
    {
        if (instructionText != null)
            instructionText.text = instruction;

        if (sentenceText != null)
            sentenceText.text = displaySentence;

        if (feedbackText != null)
            feedbackText.text = "";
    }

    /// <summary>
    /// Kaldes når VoiceMovement melder et færdigt resultat.
    /// Stopper lytningen, sammenligner barnets sætning med målsætningen,
    /// og udløser success- eller fail-event.
    /// </summary>
    public void OnSpeechRecognized(string recognizedText)
    {
        string normalizedHeard = Normalize(recognizedText);

        // Ét forsøg pr. runde – vi stopper altid mikrofonen her
        StopListening();

        if (normalizedHeard == _targetSentenceNormalized)
        {
            if (feedbackText != null)
                feedbackText.text = "Rigtigt! 🐼";

            OnTaskCompleted?.Invoke();
            // Panelet forbliver synligt – næste runde vælger selv hvad der skal ske.
        }
        else
        {
            if (feedbackText != null)
                feedbackText.text = $"Jeg hørte: \"{recognizedText}\"";

            OnTaskFailed?.Invoke();
        }
    }

    /// <summary>
    /// Gør en sætning klar til sammenligning ved at:
    /// Konvertere til små bogstaver
    /// Fjerne tegnsætning
    /// Trimme mellemrum
    /// Bruges til både barnets svar og målsætningen.
    /// </summary>
    private string Normalize(string sentence)
    {
        if (string.IsNullOrWhiteSpace(sentence))
            return "";

        sentence = sentence.ToLowerInvariant().Trim();

        // Fjern simpel tegnsætning
        sentence = sentence.Replace(".", "")
             .Replace(",", "")
             .Replace("!", "")
             .Replace("?", "");

        return sentence;
    }

    /// <summary>
    /// Blander alle ordene i sætningen tilfældigt.
    /// Forsøger flere gange at finde en version, der ikke er identisk
    /// med den oprindelige sætning. Bruges af boss-kampe.
    /// </summary>
    private string ScrambleWords(string sentence)
    {
        if (string.IsNullOrWhiteSpace(sentence))
            return sentence;

        string[] words = sentence.Split(' ');
        if (words.Length <= 1)
            return sentence;

        string originalSentence = string.Join(" ", words);

        for (int attemptIndex = 0; attemptIndex < MaximumScrambleAttempts; attemptIndex++)
        {
            // Lav en kopi som vi kan blande
            string[] shuffledWords = (string[])words.Clone();

            for (int wordIndex = 0; wordIndex < shuffledWords.Length; wordIndex++)
            {
                int randomSwapIndex = Random.Range(0, shuffledWords.Length);

                // Byt de to ord
                (shuffledWords[wordIndex], shuffledWords[randomSwapIndex]) =
                (shuffledWords[randomSwapIndex], shuffledWords[wordIndex]);
            }

            string scrambledSentence = string.Join(" ", shuffledWords);

            // Hvis resultatet er anderledes end originalen → brug det
            if (!scrambledSentence.Equals(originalSentence))
            {
                return scrambledSentence;
            }
        }

        // Hvis alle forsøg gav det samme (fx alle ord er identiske),
        // falder vi tilbage til originalen.
        return originalSentence;
    }

    /// <summary>
    /// Flytter ét enkelt ord til en anden position i sætningen.
    /// Forsøger flere gange at sikre, at resultatet ikke er identisk
    /// med originalen. Bruges af kattekampe.
    /// </summary>
    private string ScrambleOneWord(string sentence)
    {
        if (string.IsNullOrWhiteSpace(sentence))
            return sentence;

        string[] words = sentence.Split(' ');
        if (words.Length <= 1)
            return sentence;

        string originalSentence = string.Join(" ", words);

        for (int attemptIndex = 0; attemptIndex < MaximumScrambleAttempts; attemptIndex++)
        {
            var wordList = new List<string>(words);

            // Vælg ét ord at flytte
            int fromIndex = Random.Range(0, wordList.Count);
            int toIndex = fromIndex;

            // Sørg for at vi vælger en anden position
            while (toIndex == fromIndex)
            {
                toIndex = Random.Range(0, wordList.Count);
            }

            string wordToMove = wordList[fromIndex];
            wordList.RemoveAt(fromIndex);

            // Hvis vi fjerner før målet, rykker indekserne én ned
            if (toIndex > fromIndex)
            {
                toIndex--;
            }

            wordList.Insert(toIndex, wordToMove);

            string scrambledSentence = string.Join(" ", wordList);

            // Kun acceptér resultatet hvis det ikke er identisk med originalen
            if (!scrambledSentence.Equals(originalSentence))
            {
                return scrambledSentence;
            }
        }

        // Hvis vi af en eller anden grund ikke kan lave noget anderledes
        // (fx alle ord er ens), så falder vi tilbage til originalen.
        return originalSentence;
    }

    /// <summary>
    /// Starter mikrofonlytning ved at registrere en observer hos VoiceMovement.
    /// Viser en besked hvis mikrofonen ikke er tændt.
    /// I Unity Editor simuleres et forkert svar automatisk.
    /// </summary>
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

        if (_taskObserver == null)
            _taskObserver = new TaskObserver(this);

        voiceMovement.RegisterObserver(_taskObserver);

        if (!voiceMovement.isMicrophoneOn && feedbackText != null)
        {
            feedbackText.text = "Tænd mikrofonen (Mic-knappen) for at sige sætningen.";
        }
    }

    /// <summary>
    /// Stopper mikrofonen og afregistrerer observeren,
    /// så der ikke lyttes videre mellem runderne.
    /// </summary>
    private void StopListening()
    {
        if (voiceMovement == null)
            return;

        if (_taskObserver != null)
            voiceMovement.UnregisterObserver(_taskObserver);

        voiceMovement.StopMicrophone();
    }
}
