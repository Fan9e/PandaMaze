using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Attach to a parent object that contains child obstacle groups (rocks / fallen bamboo) or directly to a single obstacle.
/// The script will automatically add small trigger-forwarder components to child colliders so you can place it on the parent.
/// 
/// Features:
/// - Works when attached to a parent with many child colliders (for your Rock and Fallen_Bamboo groups).
/// - Inspector-editable keyword lists for Rocks and FallenBamboo.
/// - Optional on-screen visual prompt (TextMeshProUGUI).
/// - Adds forwarder components to child colliders in the Editor (OnValidate) so it's ready immediately after attaching.
/// </summary>
public class ObstacleVoiceGate : MonoBehaviour, IVoiceObserver
{
    public enum ObstacleKind { Rocks, FallenBamboo }

    [Header("General")]
    public ObstacleKind Kind = ObstacleKind.Rocks;

    [Tooltip("If true the script will gather child colliders and forward their trigger events to this component.")]
    public bool useChildColliders = true;

    [Header("Pass settings")]
    public float passDistance = 2.0f;       // how far forward the player is moved to get past obstacle
    public float passDuration = 0.5f;       // total duration of the pass movement
    public float overHeight = 1.2f;         // peak height for "over"
    public float underDepth = 0.5f;         // how much lower player goes for "under"
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
    public string[] rockKeywords = new[] { "jump", "over", "hop", "up" };
    public string[] bambooKeywords = new[] { "duck", "under", "crouch", "down" };

    [Header("UI Prompt (optional)")]
    [Tooltip("Optional TextMeshProUGUI prefab to use as the on-screen prompt. If null, a simple prompt is created at runtime.")]
    public TextMeshProUGUI promptPrefab;
    [Tooltip("Anchor/offset for generated prompt when no prefab is provided (screen-space overlay).")]
    public Vector2 promptAnchoredPosition = new Vector2(0, 120);
    [Tooltip("The message shown when player is in range; {0} is replaced with the keywords list, {1} with the action (\"jump/duck\").")]
    [TextArea]
    public string promptFormat = "Say one of: {0}\nTo {1} the obstacle.";

    // runtime state
    private bool playerInside = false;
    private Transform playerTransform;
    private Rigidbody playerRb;
    private PlayerMovement playerMovement;
    private VoiceMovement voiceMovement;
    private bool isPassing = false;

    private TextMeshProUGUI promptInstance;
    private Canvas overlayCanvas;

    // Called in the editor when values/changing script or when component added.
    private void OnValidate()
    {
        if (!useChildColliders) return;

        // Add forwarders and make child colliders triggers so setup is immediate in editor.
        var colliders = GetComponentsInChildren<Collider>(true);
        foreach (var col in colliders)
        {
            if (col.gameObject == gameObject) continue;

            // Safely attempt to make collider a trigger (editor change recorded for undo).
            if (!TrySetTriggerSafe(col))
                continue;

            var f = col.GetComponent<ObstacleTriggerForwarder>();
            if (f == null)
            {
#if UNITY_EDITOR
                if (!EditorApplication.isPlaying)
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

            // ensure forwarder points to this gate instance
            f.parentGate = this;
        }
    }

    private void Awake()
    {
        // if parent has its own collider, make it a trigger (safe; optional)
        var ownCol = GetComponent<Collider>();
        if (ownCol != null)
            TrySetTriggerSafe(ownCol);

        // If useChildColliders was disabled at edit time this still ensures forwarders are present at runtime.
        if (useChildColliders)
        {
            var colliders = GetComponentsInChildren<Collider>(true);
            foreach (var col in colliders)
            {
                if (col.gameObject == gameObject) continue;
                TrySetTriggerSafe(col);
                var f = col.GetComponent<ObstacleTriggerForwarder>();
                if (f == null)
                    f = col.gameObject.AddComponent<ObstacleTriggerForwarder>();
                f.parentGate = this;
            }
        }
    }

    /// <summary>
    /// Try to set a collider to be a trigger while avoiding Unity's "Triggers on concave MeshColliders are not supported" error.
    /// If the collider is a MeshCollider and not convex, we skip changing it and log a warning.
    /// Returns true if the collider was set (or already set) to trigger; false if skipped.
    /// </summary>
    private bool TrySetTriggerSafe(Collider col)
    {
        if (col == null) return false;

        var meshCol = col as MeshCollider;
        if (meshCol != null && !meshCol.convex)
        {
            // Unity throws if you set isTrigger on a concave MeshCollider. Skip and inform user.
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
        // find or create overlay canvas for prompts (screen-space overlay)
        overlayCanvas = FindObjectOfType<Canvas>();
        if (overlayCanvas == null)
        {
            var canvasGO = new GameObject("VoicePromptCanvas");
            overlayCanvas = canvasGO.AddComponent<Canvas>();
            overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();
            DontDestroyOnLoad(canvasGO);
        }
    }

    // These are called by forwarders added to child colliders
    internal void ChildTriggerEnter(GameObject childColliderOwner, Collider other) => HandleTriggerEnter(other);
    internal void ChildTriggerExit(GameObject childColliderOwner, Collider other) => HandleTriggerExit(other);

    // Keep compatibility: also support direct triggers if script is placed on the same GameObject as a collider.
    private void OnTriggerEnter(Collider other) => HandleTriggerEnter(other);
    private void OnTriggerExit(Collider other) => HandleTriggerExit(other);

    private void HandleTriggerEnter(Collider other)
    {
        if (playerInside) return;

        // detect player by existence of PlayerMovement component
        var pm = other.GetComponent<PlayerMovement>();
        if (pm == null) return;

        playerInside = true;
        playerMovement = pm;
        playerTransform = other.transform;
        playerRb = other.GetComponent<Rigidbody>();

        voiceMovement = FindObjectOfType<VoiceMovement>();
        if (voiceMovement != null)
        {
            voiceMovement.RegisterObserver(this);
            if (autoStartMicrophone)
                voiceMovement.StartMicrophone();
        }

        ShowPrompt();

        Debug.Log($"ObstacleVoiceGate: Player entered {name}. Say the correct command to pass ({Kind}).");
    }

    private void HandleTriggerExit(Collider other)
    {
        var pm = other.GetComponent<PlayerMovement>();
        if (pm == null) return;
        if (!playerInside) return;

        playerInside = false;
        if (voiceMovement != null)
            voiceMovement.UnregisterObserver(this);

        HidePrompt();

        if (!isPassing)
        {
            playerTransform = null;
            playerRb = null;
            playerMovement = null;
        }

        Debug.Log($"ObstacleVoiceGate: Player left {name}. Voice observer unregistered.");
    }

    #region IVoiceObserver
    public void OnPartialResult(string partial) { /* optional: update prompt */ }

    public void OnResult(string result)
    {
        if (!playerInside || isPassing) return;

        string spoken = (result ?? string.Empty).ToLowerInvariant().Trim();
        if (string.IsNullOrEmpty(spoken)) return;

        bool matched = false;
        if (Kind == ObstacleKind.Rocks)
        {
            foreach (var k in rockKeywords)
            {
                if (spoken.Contains(k.ToLowerInvariant()))
                {
                    matched = true;
                    StartCoroutine(PerformOver());
                    break;
                }
            }
        }
        else
        {
            foreach (var k in bambooKeywords)
            {
                if (spoken.Contains(k.ToLowerInvariant()))
                {
                    matched = true;
                    StartCoroutine(PerformUnder());
                    break;
                }
            }
        }

        if (!matched)
        {
            Debug.Log($"ObstacleVoiceGate: command not recognized for {Kind}. Spoken: '{result}'");
            if (promptInstance != null)
                promptInstance.text = $"Not recognized. {GeneratePromptKeywordsText()}";
        }

        if (voiceMovement != null)
            voiceMovement.UnregisterObserver(this);
    }

    public void OnVoiceLevelChanged(float level) { /* optional: animate prompt */ }

    public void OnMicrophoneStateChanged(bool isOn) { /* no-op */ }
    #endregion

    private IEnumerator PerformOver()
    {
        if (playerTransform == null || playerRb == null || playerMovement == null) yield break;
        isPassing = true;
        HidePrompt();

        playerMovement.enabled = false;
        bool prevKinematic = playerRb.isKinematic;
        playerRb.isKinematic = true;

        Vector3 start = playerTransform.position;
        Vector3 forward = (playerTransform.forward).normalized;
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

        // Optionally modify player's collider for a more convincing duck.
        CapsuleCollider cap = null;
        CharacterController cc = null;
        float origCapsuleHeight = 0f;
        Vector3 origCapsuleCenter = Vector3.zero;
        float targetCapsuleHeight = 0f;
        Vector3 targetCapsuleCenter = Vector3.zero;

        if (changePlayerColliderWhenDucking)
        {
            // look for CapsuleCollider or CharacterController on the player transform or its children
            cap = playerTransform.GetComponent<CapsuleCollider>() ?? playerTransform.GetComponentInChildren<CapsuleCollider>();
            cc = playerTransform.GetComponent<CharacterController>() ?? playerTransform.GetComponentInChildren<CharacterController>();

            if (cap != null)
            {
                origCapsuleHeight = cap.height;
                origCapsuleCenter = cap.center;
                targetCapsuleHeight = Mathf.Max(0.01f, origCapsuleHeight * duckColliderHeightMultiplier);
                // move center down so bottom of collider stays roughly at same world position
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
        Vector3 forward = (playerTransform.forward).normalized;
        Vector3 end = start + forward * passDistance;
        float elapsed = 0f;

        // Animate collider down during the first colliderChangeDuration, restore after movement.
        while (elapsed < passDuration)
        {
            float t = Mathf.Clamp01(elapsed / passDuration);
            Vector3 horiz = Vector3.Lerp(start, end, t);
            float dip = -4f * underDepth * t * (1 - t);
            playerTransform.position = new Vector3(horiz.x, horiz.y + dip, horiz.z);

            // collider lerp (down)
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

        // Ensure final position
        playerTransform.position = end;

        // restore collider smoothly
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

            // finalize exact values
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
        string[] arr = Kind == ObstacleKind.Rocks ? rockKeywords : bambooKeywords;
        if (arr == null || arr.Length == 0) return "(no keywords)";
        return string.Join(", ", arr);
    }
    #endregion

    // Forwarder helper: added to child collider GameObjects so parent's methods are called.
    [DisallowMultipleComponent]
    private class ObstacleTriggerForwarder : MonoBehaviour
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