using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Monster))]
public class MonsterSentenceTask : MonoBehaviour, IVoiceObserver
{
    [Header("Sætninger til dette monster")]
    [TextArea]
    [SerializeField] private string[] sentences;

    [Header("Referencer")]
    [SerializeField] private SentenceTaskUI sentenceTaskUI;
    [SerializeField] private PlayerWeapon playerWeapon;
    [SerializeField] private VoiceMovement voiceMovement;
    [SerializeField] private float triggerRadius = 3f;

    [Header("Kamp / skade")]
    [SerializeField] private int damageOnCorrectSentence = 10;

    private Monster _monster;
    private Transform _player;

    private bool _taskActive;

    private List<int> _remainingSentenceIndices;
    private string _expectedSentence;

    private void Awake()
    {
        _monster = GetComponent<Monster>();

        if (sentences == null || sentences.Length == 0)
        {
            sentences = new[]
            {
                "Jeg har set en pirat",
                "Pirater er seje",
                "Jeg kan godt lide pirater"
            };
        }

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            _player = playerObj.transform;
    }

    private void OnEnable()
    {
        if (voiceMovement != null)
            voiceMovement.RegisterObserver(this);
    }

    private void OnDisable()
    {
        if (voiceMovement != null)
            voiceMovement.UnregisterObserver(this);
    }

    private void Update()
    {
        if (_taskActive) return;
        if (_player == null) return;

        float distance = Vector3.Distance(_player.position, transform.position);
        if (distance <= triggerRadius)
        {
            StartTask();
        }
    }

    private void StartTask()
    {
        if (sentences == null || sentences.Length == 0)
            return;

        _taskActive = true;

        _remainingSentenceIndices = new List<int>();
        for (int i = 0; i < sentences.Length; i++)
            _remainingSentenceIndices.Add(i);

        ShowNextRandomSentence();
    }

    private void ShowNextRandomSentence()
    {
        if (_remainingSentenceIndices.Count == 0)
        {
            EndTask();
            return;
        }

        int pick = Random.Range(0, _remainingSentenceIndices.Count);
        int chosenIndex = _remainingSentenceIndices[pick];
        _remainingSentenceIndices.RemoveAt(pick);

        _expectedSentence = sentences[chosenIndex];

        if (sentenceTaskUI != null)
            sentenceTaskUI.ShowSentence(_expectedSentence);

        if (voiceMovement != null && !voiceMovement.isMicrophoneOn)
            voiceMovement.StartMicrophone();
    }

    private void HandleVoiceResult(bool isCorrect)
    {
        if (!_taskActive) return;

        if (isCorrect)
        {
            Debug.Log("✅ Korrekt sætning – skade!");
            if (_monster != null)
                _monster.TakeDamage(damageOnCorrectSentence);

            if (playerWeapon != null && _monster != null)
                playerWeapon.AttackSpecificMonster(_monster);

            if (_remainingSentenceIndices.Count == 0)
                EndTask();
            else
                ShowNextRandomSentence();
        }
        else
        {
            Debug.Log("❌ Forkert sætning");
        }
    }

    private void EndTask()
    {
        _taskActive = false;
        _expectedSentence = null;

        if (sentenceTaskUI != null)
            sentenceTaskUI.Hide();
    }

    // ============================================================
    //  VOICE OBSERVER
    // ============================================================

    public void OnPartialResult(string partial)
    {
        // Ikke vigtigt lige nu
    }

    public void OnResult(string result)
    {
        Debug.Log($"[MonsterSentenceTask] OnResult called. _taskActive={_taskActive}, result='{result}'");

        if (string.IsNullOrWhiteSpace(result))
            result = "";

        string lower = result.ToLowerInvariant();

        // SUPER simpel: Hvis der siges noget med "pirat", så er det korrekt
        bool isCorrect = lower.Contains("pirat");

        Debug.Log($"[MonsterSentenceTask] isCorrect = {isCorrect}");

        HandleVoiceResult(isCorrect);
    }

    public void OnVoiceLevelChanged(float level) { }
    public void OnMicrophoneStateChanged(bool isOn) { }
}
