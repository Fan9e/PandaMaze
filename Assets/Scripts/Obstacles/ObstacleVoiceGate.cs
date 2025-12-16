using System.Collections;
using System.Linq;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class ObstacleVoiceGate : MonoBehaviour, IVoiceObserver
{
    public enum ObstacleKind { Rocks, FallenBamboo }

    [Header("General")]
    public ObstacleKind Kind = ObstacleKind.Rocks;

    [Tooltip("If true the script will gather child colliders and forward their trigger events to this component.")]
    public bool useChildColliders = true;

    [Header("Pass settings")]
    public float passDistance = 3.5f;
    public float passDuration = 0.5f;
    public float overHeight = 1.5f;
    public float underDepth = 0.5f;
    [Tooltip("If true will attempt to start the microphone automatically when the player enters.")]
    public bool autoStartMicrophone = true;

    [Header("Collider Ducking (optional)")]
    [Tooltip("If true will temporarily reduce player's CapsuleCollider/CharacterController height while performing an 'under' pass.")]
    public bool changePlayerColliderWhenDucking = true;
    [Tooltip("Multiplier applied to collider height when ducking (0..1). Typical 0.4-0.7.")]
    public float duckColliderHeightMultiplier = 0.5f;
    [Tooltip("Duration (seconds) for the collider height change to lerp down/up.")]
    public float colliderChangeDuration = 0.12f;

    [Header("Keywords (editable)")]
    public string[] rockKeywords = new[] { "jump", "over", "hop", "up", "Hello world" };
    public string[] bambooKeywords = new[] { "duck", "under", "crouch", "down", "Hello world" };

    [Header("UI Prompt (optional)")]
    [Tooltip("Optional TextMeshProUGUI prefab to use as the on-screen prompt. If null, a simple prompt is created at runtime.")]
    public TextMeshProUGUI promptPrefab;
    [Tooltip("Anchor/offset for generated prompt when no prefab is provided (screen-space overlay).")]
    public Vector2 promptAnchoredPosition = new Vector2(0, 120);
    [Tooltip("The message shown when player is in range; {0} is replaced with the keywords list, {1} with the action (\"jump/duck\").")]
    [TextArea]
    public string promptFormat = "Say one of: {0}\nTo {1} the obstacle.";

    private bool playerInside = false;
    private Transform playerTransform;
    private Rigidbody playerRb;
    private PlayerMovement playerMovement;
    private VoiceMovement voiceMovement;
    private bool isPassing = false;

    private TextMeshProUGUI promptInstance;
    private Canvas overlayCanvas;

    private void OnValidate()
    {
        if (!useChildColliders) return;

#if UNITY_EDITOR
        if (!EditorApplication.isPlaying)
            EnsureChildForwarders(useUndo: true);
        else
            EnsureChildForwarders(useUndo: false);
#else
        EnsureChildForwarders(useUndo: false);
#endif
    }

    private void Awake()
    {
        var ownCol = GetComponent<Collider>();
        if (ownCol != null)
            TrySetTriggerSafe(ownCol);

        if (useChildColliders)
            EnsureChildForwarders(useUndo: false);
    }

    private void EnsureChildForwarders(bool useUndo)
    {
        var colliders = GetComponentsInChildren<Collider>(true);
        foreach (var col in colliders)
        {
            if (col == null || col.gameObject == gameObject) continue;

            if (!TrySetTriggerSafe(col))
                continue;

            var f = col.GetComponent<ObstacleTriggerForwarder>();
            if (f == null)
            {
#if UNITY_EDITOR
                if (useUndo && !EditorApplication.isPlaying)
                {
                    f = Undo.AddComponent<ObstacleTriggerForwarder>(col.gameObject);
                }
                else
                {
                    f = col.gameObject.AddComponent<ObstacleTriggerForwarder>();
                }
#else
                f = col.gameObject.AddComponent<ObstacleTriggerForwarder>();
#endif
            }

            f.parentGate = this;
        }
    }

    private bool TrySetTriggerSafe(Collider col)
    {
        if (col == null) return false;

        var meshCol = col as MeshCollider;
        if (meshCol != null && !meshCol.convex)
        {
            Debug.LogWarning($"ObstacleVoiceGate: Skipping making concave MeshCollider '{col.name}' a trigger. Unity does not support triggers on concave MeshColliders. Make the MeshCollider convex or use primitive colliders (Box/Sphere/Capsule) instead.", col.gameObject);
            return false;
        }

#if UNITY_EDITOR
        if (!EditorApplication.isPlaying)
        {
            Undo.RecordObject(col, "Set collider to trigger");
            col.isTrigger = true;
        }
        else
        {
            col.isTrigger = true;
        }
#else
        col.isTrigger = true;
#endif
        return true;
    }

    private void OnEnable()
    {
        overlayCanvas = FindObjectOfType<Canvas>();
        if (overlayCanvas == null)
            overlayCanvas = CreateOverlayCanvas();
    }

    private Canvas CreateOverlayCanvas()
    {
        var canvasGO = new GameObject("VoicePromptCanvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();
        DontDestroyOnLoad(canvasGO);
        return canvas;
    }

    internal void ChildTriggerEnter(GameObject childColliderOwner, Collider other) => HandleTriggerEnter(other);
    internal void ChildTriggerExit(GameObject childColliderOwner, Collider other) => HandleTriggerExit(other);

    private void OnTriggerEnter(Collider other) => HandleTriggerEnter(other);
    private void OnTriggerExit(Collider other) => HandleTriggerExit(other);

    private void HandleTriggerEnter(Collider other)
    {
        if (playerInside) return;
        if (!TryGetPlayerFromCollider(other, out var pm, out var rb)) return;

        playerInside = true;
        playerMovement = pm;
        playerTransform = other.transform;
        playerRb = rb;

        RegisterVoiceObserver();

        ShowPrompt();

        Debug.Log($"ObstacleVoiceGate: Player entered {name}. Say the correct command to pass ({Kind}).");
    }

    private void HandleTriggerExit(Collider other)
    {
        if (!TryGetPlayerFromCollider(other, out var pm, out var rb)) return;
        if (!playerInside) return;

        playerInside = false;
        UnregisterVoiceObserver();

        HidePrompt();

        if (!isPassing)
        {
            playerTransform = null;
            playerRb = null;
            playerMovement = null;
        }

        Debug.Log($"ObstacleVoiceGate: Player left {name}. Voice observer unregistered.");
    }

    private bool TryGetPlayerFromCollider(Collider other, out PlayerMovement pm, out Rigidbody rb)
    {
        pm = other.GetComponent<PlayerMovement>();
        if (pm == null)
        {
            rb = null;
            return false;
        }

        rb = other.GetComponent<Rigidbody>();
        return true;
    }

    private void RegisterVoiceObserver()
    {
        voiceMovement = FindObjectOfType<VoiceMovement>();
        if (voiceMovement == null) return;

        voiceMovement.RegisterObserver(this);
        if (autoStartMicrophone)
            voiceMovement.StartMicrophone();
    }

    private void UnregisterVoiceObserver()
    {
        if (voiceMovement != null)
            voiceMovement.UnregisterObserver(this);
        voiceMovement = null;
    }

    #region IVoiceObserver
    public void OnPartialResult(string partial) { }

    public void OnResult(string result)
    {
        if (!playerInside || isPassing) return;

        string spoken = (result ?? string.Empty).ToLowerInvariant().Trim();
        if (string.IsNullOrEmpty(spoken)) return;

        if (MatchesKeywords(spoken, out var isOverAction))
        {
            if (isOverAction)
                StartCoroutine(PerformOver());
            else
                StartCoroutine(PerformUnder());
        }
        else
        {
            Debug.Log($"ObstacleVoiceGate: command not recognized for {Kind}. Spoken: '{result}'");
            if (promptInstance != null)
                promptInstance.text = $"Not recognized. {GeneratePromptKeywordsText()}";
        }

        UnregisterVoiceObserver();
    }

    public void OnVoiceLevelChanged(float level) { }

    public void OnMicrophoneStateChanged(bool isOn) { }
    #endregion

    private bool MatchesKeywords(string spoken, out bool isOverAction)
    {
        isOverAction = false;
        if (Kind == ObstacleKind.Rocks)
        {
            if (rockKeywords != null && rockKeywords.Any(k => !string.IsNullOrEmpty(k) && spoken.Contains(k.ToLowerInvariant())))
            {
                isOverAction = true;
                return true;
            }
        }
        else
        {
            if (bambooKeywords != null && bambooKeywords.Any(k => !string.IsNullOrEmpty(k) && spoken.Contains(k.ToLowerInvariant())))
            {
                isOverAction = false;
                return true;
            }
        }

        return false;
    }

    private IEnumerator PerformOver()
    {
        if (playerTransform == null || playerRb == null || playerMovement == null) yield break;
        isPassing = true;
        HidePrompt();

        playerMovement.enabled = false;
        bool prevKinematic = playerRb.isKinematic;
        playerRb.isKinematic = true;

        Vector3 start = playerTransform.position;
        Vector3 forward = playerTransform.forward.normalized;
        Vector3 end = start + forward * passDistance;
        float elapsed = 0f;

        while (elapsed < passDuration)
        {
            float t = Mathf.Clamp01(elapsed / passDuration);
            Vector3 horiz = Vector3.Lerp(start, end, t);
            float height = 4f * overHeight * t * (1 - t);
            playerTransform.position = new Vector3(horiz.x, horiz.y + height, horiz.z);
            elapsed += Time.deltaTime;
            yield return null;
        }

        playerTransform.position = end;
        playerRb.isKinematic = prevKinematic;
        playerMovement.enabled = true;
        isPassing = false;
    }

    private IEnumerator PerformUnder()
    {
        if (playerTransform == null || playerRb == null || playerMovement == null) yield break;
        isPassing = true;
        HidePrompt();

        playerMovement.enabled = false;
        bool prevKinematic = playerRb.isKinematic;
        playerRb.isKinematic = true;

        CapsuleCollider cap = null;
        CharacterController cc = null;
        float origCapsuleHeight = 0f;
        Vector3 origCapsuleCenter = Vector3.zero;
        float targetCapsuleHeight = 0f;
        Vector3 targetCapsuleCenter = Vector3.zero;

        if (changePlayerColliderWhenDucking)
        {
            cap = playerTransform.GetComponent<CapsuleCollider>() ?? playerTransform.GetComponentInChildren<CapsuleCollider>();
            cc = playerTransform.GetComponent<CharacterController>() ?? playerTransform.GetComponentInChildren<CharacterController>();

            if (cap != null)
            {
                origCapsuleHeight = cap.height;
                origCapsuleCenter = cap.center;
                targetCapsuleHeight = Mathf.Max(0.01f, origCapsuleHeight * duckColliderHeightMultiplier);
                float delta = (origCapsuleHeight - targetCapsuleHeight) * 0.5f;
                targetCapsuleCenter = origCapsuleCenter - new Vector3(0f, delta, 0f);
            }
            else if (cc != null)
            {
                origCapsuleHeight = cc.height;
                origCapsuleCenter = cc.center;
                targetCapsuleHeight = Mathf.Max(0.01f, origCapsuleHeight * duckColliderHeightMultiplier);
                float delta = (origCapsuleHeight - targetCapsuleHeight) * 0.5f;
                targetCapsuleCenter = origCapsuleCenter - new Vector3(0f, delta, 0f);
            }
        }

        Vector3 start = playerTransform.position;
        Vector3 forward = playerTransform.forward.normalized;
        Vector3 end = start + forward * passDistance;
        float elapsed = 0f;

        while (elapsed < passDuration)
        {
            float t = Mathf.Clamp01(elapsed / passDuration);
            Vector3 horiz = Vector3.Lerp(start, end, t);
            float dip = -4f * underDepth * t * (1 - t);
            playerTransform.position = new Vector3(horiz.x, horiz.y + dip, horiz.z);

            if (changePlayerColliderWhenDucking && (cap != null || cc != null))
            {
                float colT = Mathf.Clamp01(elapsed / colliderChangeDuration);
                if (cap != null)
                {
                    cap.height = Mathf.Lerp(origCapsuleHeight, targetCapsuleHeight, colT);
                    cap.center = Vector3.Lerp(origCapsuleCenter, targetCapsuleCenter, colT);
                }
                else if (cc != null)
                {
                    cc.height = Mathf.Lerp(origCapsuleHeight, targetCapsuleHeight, colT);
                    cc.center = Vector3.Lerp(origCapsuleCenter, targetCapsuleCenter, colT);
                }
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        playerTransform.position = end;

        if (changePlayerColliderWhenDucking && (cap != null || cc != null))
        {
            float restoreElapsed = 0f;
            while (restoreElapsed < colliderChangeDuration)
            {
                float rt = Mathf.Clamp01(restoreElapsed / colliderChangeDuration);
                if (cap != null)
                {
                    cap.height = Mathf.Lerp(targetCapsuleHeight, origCapsuleHeight, rt);
                    cap.center = Vector3.Lerp(targetCapsuleCenter, origCapsuleCenter, rt);
                }
                else if (cc != null)
                {
                    cc.height = Mathf.Lerp(targetCapsuleHeight, origCapsuleHeight, rt);
                    cc.center = Vector3.Lerp(targetCapsuleCenter, origCapsuleCenter, rt);
                }

                restoreElapsed += Time.deltaTime;
                yield return null;
            }

            if (cap != null)
            {
                cap.height = origCapsuleHeight;
                cap.center = origCapsuleCenter;
            }
            else if (cc != null)
            {
                cc.height = origCapsuleHeight;
                cc.center = origCapsuleCenter;
            }
        }

        playerRb.isKinematic = prevKinematic;
        playerMovement.enabled = true;
        isPassing = false;
    }

    #region Prompt UI helpers
    private void ShowPrompt()
    {
        if (overlayCanvas == null) return;

        if (promptInstance == null)
        {
            if (promptPrefab != null)
            {
                promptInstance = Instantiate(promptPrefab, overlayCanvas.transform);
            }
            else
            {
                var go = new GameObject("VoicePrompt");
                go.transform.SetParent(overlayCanvas.transform, false);
                promptInstance = go.AddComponent<TextMeshProUGUI>();
                promptInstance.fontSize = 28;
                promptInstance.alignment = TextAlignmentOptions.Center;
                promptInstance.color = Color.yellow;

                var rt = promptInstance.rectTransform;
                rt.anchorMin = new Vector2(0.5f, 0);
                rt.anchorMax = new Vector2(0.5f, 0);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = promptAnchoredPosition;
                rt.sizeDelta = new Vector2(800, 100);
            }
        }

        string keywordsText = GeneratePromptKeywordsText();
        string action = Kind == ObstacleKind.Rocks ? "jump/over" : "duck/under";
        promptInstance.text = string.Format(promptFormat, keywordsText, action);
        promptInstance.gameObject.SetActive(true);
    }

    private void HidePrompt()
    {
        if (promptInstance != null)
            promptInstance.gameObject.SetActive(false);
    }

    private string GeneratePromptKeywordsText()
    {
        var arr = Kind == ObstacleKind.Rocks ? rockKeywords : bambooKeywords;
        if (arr == null || arr.Length == 0) return "(no keywords)";
        return string.Join(", ", arr);
    }
    #endregion

    [DisallowMultipleComponent]
    private sealed class ObstacleTriggerForwarder : MonoBehaviour
    {
        internal ObstacleVoiceGate parentGate;

        private void OnTriggerEnter(Collider other)
        {
            parentGate?.ChildTriggerEnter(gameObject, other);
        }

        private void OnTriggerExit(Collider other)
        {
            parentGate?.ChildTriggerExit(gameObject, other);
        }
    }
}