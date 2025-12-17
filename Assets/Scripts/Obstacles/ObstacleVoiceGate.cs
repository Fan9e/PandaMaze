using System.Collections;
using System.Linq;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Styrer en "voice gate" for forhindringer hvor spilleren skal sige et nøgleord for at passere.
/// Registrerer sig som observer på <see cref="VoiceMovement"/> når spilleren er indenfor triggeren,
/// viser en valgfri UI-prompt og udfører en "over" eller "under" bevægelse baseret på genkendt tale.
/// </summary>
public class ObstacleVoiceGate : MonoBehaviour, IVoiceObserver
{
    /// <summary>
    /// Typer af forhindringer som bestemmer hvilken handling der forventes (hoppe eller dykke).
    /// </summary>
    public enum ObstacleKind { Rocks, FallenBamboo }

    [Header("General")]
    /// <summary>
    /// Angiver hvilken slags forhindring dette gate repræsenterer.
    /// </summary>
    public ObstacleKind Kind = ObstacleKind.Rocks;

    [Tooltip("If true the script will gather child colliders and forward their trigger events to this component.")]
    /// <summary>
    /// Hvis sandt samles child-colliders og deres trigger-events videresendes til denne komponent.
    /// </summary>
    public bool useChildColliders = true;

    [Header("Pass settings")]
    /// <summary>
    /// Horisontal afstand spilleren flyttes ved et vellykket pass (meter).
    /// </summary>
    public float passDistance = 3.5f;
    /// <summary>
    /// Varighed af pass-bevægelsen i sekunder.
    /// </summary>
    public float passDuration = 0.5f;
    /// <summary>
    /// Maks højde (over) ved et "over"-pass.
    /// </summary>
    public float overHeight = 1.5f;
    /// <summary>
    /// Maks dybde (under) ved et "under"-pass.
    /// </summary>
    public float underDepth = 0.5f;
    [Tooltip("If true will attempt to start the microphone automatically when the player enters.")]
    /// <summary>
    /// Hvis sandt forsøges mikrofonen startet automatisk når spilleren går ind i triggeren.
    /// </summary>
    public bool autoStartMicrophone = true;

    [Header("Collider Ducking (optional)")]
    [Tooltip("If true will temporarily reduce player's CapsuleCollider/CharacterController height while performing an 'under' pass.")]
    /// <summary>
    /// Hvis sandt ændres spillerens collider midlertidigt ved et "under"-pass for at simulere ducking.
    /// </summary>
    public bool changePlayerColliderWhenDucking = true;
    [Tooltip("Multiplier applied to collider height when ducking (0..1). Typical 0.4-0.7.")]
    /// <summary>
    /// Multiplikator anvendt på collider-højden ved ducking (værdi mellem 0 og 1).
    /// </summary>
    public float duckColliderHeightMultiplier = 0.5f;
    [Tooltip("Duration (seconds) for the collider height change to lerp down/up.")]
    /// <summary>
    /// Tid i sekunder for interpolation af collider-højde ned/op.
    /// </summary>
    public float colliderChangeDuration = 0.12f;

    [Header("Keywords (editable)")]
    /// <summary>
    /// Liste af nøgleord som genkendes til "over"-handling for Rocks.
    /// </summary>
    public string[] rockKeywords = new[] { "over", "hop", "op", "Hello world" };
    /// <summary>
    /// Liste af nøgleord som genkendes til "under"-handling for FallenBamboo.
    /// </summary>
    public string[] bambooKeywords = new[] { "duk", "under", "ned", "Hello world" };

    [Header("UI Prompt (optional)")]
    [Tooltip("Optional TextMeshProUGUI prefab to use as the on-screen prompt. If null, a simple prompt is created at runtime.")]
    /// <summary>
    /// Valgfri TextMeshProUGUI-prefab der bruges som on-screen prompt. Hvis null oprettes en simpel prompt ved runtime.
    /// </summary>
    public TextMeshProUGUI promptPrefab;
    [Tooltip("Anchor/offset for generated prompt when no prefab is provided (screen-space overlay).")]
    /// <summary>
    /// Anchor/offset for den genererede prompt når ingen prefab er angivet (screen-space overlay).
    /// </summary>
    public Vector2 promptAnchoredPosition = new Vector2(0, 120);
    [Tooltip("The message shown when player is in range; {0} is replaced with the keywords list, {1} with the action (\"jump/duck\").")]
    [TextArea]
    /// <summary>
    /// Formatstrengen for prompten. {0} erstattes af nøgleordene, {1} af handlingen (f.eks. "jump/over" eller "duck/under").
    /// </summary>
    public string promptFormat = "Sig en af følgende: {0}\nTil {1} forhindringen.";

    private bool playerInside = false;
    private Transform playerTransform;
    private Rigidbody playerRb;
    private PlayerMovement playerMovement;
    private VoiceMovement voiceMovement;
    private bool isPassing = false;

    private TextMeshProUGUI promptInstance;
    private Canvas overlayCanvas;

    /// <summary>
    /// Unity callback kaldt i editor/inspektør når værdier ændres — sikrer at child forwarders oprettes korrekt.
    /// </summary>
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

    /// <summary>
    /// Unity Awake. Sætter egen collider til trigger hvis relevant og opretter child forwarders hvis aktiveret.
    /// </summary>
    private void Awake()
    {
        var ownCollider = GetComponent<Collider>();
        if (ownCollider != null)
            TrySetTriggerSafe(ownCollider);

        if (useChildColliders)
            EnsureChildForwarders(useUndo: false);
    }

    /// <summary>
    /// Opretter eller genbruger ObstacleTriggerForwarder komponenter på child-colliders.
    /// </summary>
    /// <param name="useUndo">Hvis sandt bruges Undo.AddComponent i editoren (nyttigt ved redigering).</param>
    private void EnsureChildForwarders(bool useUndo)
    {
        var colliders = GetComponentsInChildren<Collider>(true);
        foreach (var childCollider in colliders)
        {
            if (childCollider == null || childCollider.gameObject == gameObject) continue;

            if (!TrySetTriggerSafe(childCollider))
                continue;

            var forwarder = childCollider.GetComponent<ObstacleTriggerForwarder>();
            if (forwarder == null)
            {
#if UNITY_EDITOR
                if (useUndo && !EditorApplication.isPlaying)
                {
                    forwarder = Undo.AddComponent<ObstacleTriggerForwarder>(childCollider.gameObject);
                }
                else
                {
                    forwarder = childCollider.gameObject.AddComponent<ObstacleTriggerForwarder>();
                }
#else
                forwarder = childCollider.gameObject.AddComponent<ObstacleTriggerForwarder>();
#endif
            }

            forwarder.parentGate = this;
        }
    }

    /// <summary>
    /// Prøver sikkert at sætte en collider til trigger. Returnerer false hvis f.eks. en concave MeshCollider findes.
    /// </summary>
    /// <param name="colliderToSet">Collider der forsøges ændret.</param>
    /// <returns>True hvis collider er sat til trigger, ellers false.</returns>
    private bool TrySetTriggerSafe(Collider colliderToSet)
    {
        if (colliderToSet == null) return false;

        var meshCollider = colliderToSet as MeshCollider;
        if (meshCollider != null && !meshCollider.convex)
        {
            Debug.LogWarning($"ObstacleVoiceGate: Skipping making concave MeshCollider '{colliderToSet.name}' a trigger. Unity does not support triggers on concave MeshColliders. Make the MeshCollider convex or use primitive colliders (Box/Sphere/Capsule) instead.", colliderToSet.gameObject);
            return false;
        }

#if UNITY_EDITOR
        if (!EditorApplication.isPlaying)
        {
            Undo.RecordObject(colliderToSet, "Set collider to trigger");
            colliderToSet.isTrigger = true;
        }
        else
        {
            colliderToSet.isTrigger = true;
        }
#else
        colliderToSet.isTrigger = true;
#endif
        return true;
    }

    /// <summary>
    /// Unity OnEnable. Finder eller opretter overlay-canvas til prompt UI.
    /// </summary>
    private void OnEnable()
    {
        overlayCanvas = FindObjectOfType<Canvas>();
        if (overlayCanvas == null)
            overlayCanvas = CreateOverlayCanvas();
    }

    /// <summary>
    /// Opretter en simpel overlay Canvas som ikke destrueres på sceneskift.
    /// </summary>
    /// <returns>Den oprettede Canvas.</returns>
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

    /// <summary>
    /// Modtager forwarded trigger-enter fra child forwarders.
    /// </summary>
    internal void ChildTriggerEnter(GameObject childColliderOwner, Collider other) => HandleTriggerEnter(other);
    /// <summary>
    /// Modtager forwarded trigger-exit fra child forwarders.
    /// </summary>
    internal void ChildTriggerExit(GameObject childColliderOwner, Collider other) => HandleTriggerExit(other);

    private void OnTriggerEnter(Collider other) => HandleTriggerEnter(other);
    private void OnTriggerExit(Collider other) => HandleTriggerExit(other);

    /// <summary>
    /// Håndterer når noget går ind i triggeren. Registrerer spiller og voice-observer hvis det er spilleren.
    /// </summary>
    /// <param name="other">Collider der triggede.</param>
    private void HandleTriggerEnter(Collider other)
    {
        if (playerInside) return;
        if (!TryGetPlayerFromCollider(other, out var foundPlayerMovement, out var foundRigidbody)) return;

        playerInside = true;
        playerMovement = foundPlayerMovement;
        playerTransform = other.transform;
        playerRb = foundRigidbody;

        RegisterVoiceObserver();

        ShowPrompt();

        Debug.Log($"ObstacleVoiceGate: Player entered {name}. Say the correct command to pass ({Kind}).");
    }

    /// <summary>
    /// Håndterer når noget forlader triggeren. Afregistrerer voice-observer hvis det er spilleren.
    /// </summary>
    /// <param name="other">Collider der forlod triggeren.</param>
    private void HandleTriggerExit(Collider other)
    {
        if (!TryGetPlayerFromCollider(other, out var foundPlayerMovement, out var foundRigidbody)) return;
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

    /// <summary>
    /// Forsøger at hente PlayerMovement og Rigidbody fra en collider.
    /// </summary>
    /// <param name="collider">Collider der skal tjekkes.</param>
    /// <param name="foundPlayerMovement">Udgående fundet PlayerMovement (eller null).</param>
    /// <param name="foundRigidbody">Udgående fundet Rigidbody (eller null).</param>
    /// <returns>True hvis PlayerMovement blev fundet, ellers false.</returns>
    private bool TryGetPlayerFromCollider(Collider collider, out PlayerMovement foundPlayerMovement, out Rigidbody foundRigidbody)
    {
        foundPlayerMovement = collider.GetComponent<PlayerMovement>();
        if (foundPlayerMovement == null)
        {
            foundRigidbody = null;
            return false;
        }

        foundRigidbody = collider.GetComponent<Rigidbody>();
        return true;
    }

    /// <summary>
    /// Finder VoiceMovement i scenen og registrerer denne gate som observer. Starter mikrofon hvis aktiveret.
    /// </summary>
    private void RegisterVoiceObserver()
    {
        voiceMovement = FindObjectOfType<VoiceMovement>();
        if (voiceMovement == null) return;

        voiceMovement.RegisterObserver(this);
        if (autoStartMicrophone)
            voiceMovement.StartMicrophone();
    }

    /// <summary>
    /// Afregistrerer denne gate fra VoiceMovement-observatører.
    /// </summary>
    private void UnregisterVoiceObserver()
    {
        if (voiceMovement != null)
            voiceMovement.UnregisterObserver(this);
        voiceMovement = null;
    }

    #region IVoiceObserver
    /// <summary>
    /// Delresultat fra talegenkendelse. Ikke brugt i denne implementation.
    /// </summary>
    /// <param name="partial">Delvist genkendt tekst.</param>
    public void OnPartialResult(string partial) { }

    /// <summary>
    /// Endeligt resultat fra talegenkendelse. Starter pass hvis et nøgleord genkendes.
    /// </summary>
    /// <param name="result">Den genkendte sætning.</param>
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
                promptInstance.text = $"Ikke genkendt. {GeneratePromptKeywordsText()}";
        }

        UnregisterVoiceObserver();
    }

    /// <summary>
    /// Kaldes når mikrofonens lydniveau ændres. Ikke brugt her, men kræves af IVoiceObserver.
    /// </summary>
    /// <param name="level">Lydniveau (0..1).</param>
    public void OnVoiceLevelChanged(float level) { }

    /// <summary>
    /// Kaldes når mikrofonens tilstand (on/off) ændres. Ikke brugt her.
    /// </summary>
    /// <param name="isOn">True hvis mikrofonen er tændt.</param>
    public void OnMicrophoneStateChanged(bool isOn) { }
    #endregion

    /// <summary>
    /// Tjekker om den talte tekst indeholder et af de konfigurerede nøgleord.
    /// </summary>
    /// <param name="spoken">Den talte tekst (forventes allerede lowercase/trimmet).</param>
    /// <param name="isOverAction">Udgående: true hvis det matcher en "over"-handling.</param>
    /// <returns>True hvis et match blev fundet, ellers false.</returns>
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

    /// <summary>
    /// Coroutine der udfører en "over" bevægelse (hop) for spilleren.
    /// </summary>
    /// <returns>IEnumerator til brug med StartCoroutine.</returns>
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

    /// <summary>
    /// Coroutine der udfører en "under" bevægelse (duck) for spilleren, inklusive midlertidig collider-ændring hvis aktiveret.
    /// </summary>
    /// <returns>IEnumerator til brug med StartCoroutine.</returns>
    private IEnumerator PerformUnder()
    {
        if (playerTransform == null || playerRb == null || playerMovement == null) yield break;
        isPassing = true;
        HidePrompt();

        playerMovement.enabled = false;
        bool prevKinematic = playerRb.isKinematic;
        playerRb.isKinematic = true;

        CapsuleCollider capsuleCollider = null;
        CharacterController characterController = null;
        float origCapsuleHeight = 0f;
        Vector3 origCapsuleCenter = Vector3.zero;
        float targetCapsuleHeight = 0f;
        Vector3 targetCapsuleCenter = Vector3.zero;

        if (changePlayerColliderWhenDucking)
        {
            capsuleCollider = playerTransform.GetComponent<CapsuleCollider>() ?? playerTransform.GetComponentInChildren<CapsuleCollider>();
            characterController = playerTransform.GetComponent<CharacterController>() ?? playerTransform.GetComponentInChildren<CharacterController>();

            if (capsuleCollider != null)
            {
                origCapsuleHeight = capsuleCollider.height;
                origCapsuleCenter = capsuleCollider.center;
                targetCapsuleHeight = Mathf.Max(0.01f, origCapsuleHeight * duckColliderHeightMultiplier);
                float delta = (origCapsuleHeight - targetCapsuleHeight) * 0.5f;
                targetCapsuleCenter = origCapsuleCenter - new Vector3(0f, delta, 0f);
            }
            else if (characterController != null)
            {
                origCapsuleHeight = characterController.height;
                origCapsuleCenter = characterController.center;
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

            if (changePlayerColliderWhenDucking && (capsuleCollider != null || characterController != null))
            {
                float colT = Mathf.Clamp01(elapsed / colliderChangeDuration);
                if (capsuleCollider != null)
                {
                    capsuleCollider.height = Mathf.Lerp(origCapsuleHeight, targetCapsuleHeight, colT);
                    capsuleCollider.center = Vector3.Lerp(origCapsuleCenter, targetCapsuleCenter, colT);
                }
                else if (characterController != null)
                {
                    characterController.height = Mathf.Lerp(origCapsuleHeight, targetCapsuleHeight, colT);
                    characterController.center = Vector3.Lerp(origCapsuleCenter, targetCapsuleCenter, colT);
                }
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        playerTransform.position = end;

        if (changePlayerColliderWhenDucking && (capsuleCollider != null || characterController != null))
        {
            float restoreElapsed = 0f;
            while (restoreElapsed < colliderChangeDuration)
            {
                float rt = Mathf.Clamp01(restoreElapsed / colliderChangeDuration);
                if (capsuleCollider != null)
                {
                    capsuleCollider.height = Mathf.Lerp(targetCapsuleHeight, origCapsuleHeight, rt);
                    capsuleCollider.center = Vector3.Lerp(targetCapsuleCenter, origCapsuleCenter, rt);
                }
                else if (characterController != null)
                {
                    characterController.height = Mathf.Lerp(targetCapsuleHeight, origCapsuleHeight, rt);
                    characterController.center = Vector3.Lerp(targetCapsuleCenter, origCapsuleCenter, rt);
                }

                restoreElapsed += Time.deltaTime;
                yield return null;
            }

            if (capsuleCollider != null)
            {
                capsuleCollider.height = origCapsuleHeight;
                capsuleCollider.center = origCapsuleCenter;
            }
            else if (characterController != null)
            {
                characterController.height = origCapsuleHeight;
                characterController.center = origCapsuleCenter;
            }
        }

        playerRb.isKinematic = prevKinematic;
        playerMovement.enabled = true;
        isPassing = false;
    }

    #region Prompt UI helpers
    /// <summary>
    /// Viser prompten (opretter en instans hvis nødvendig).
    /// </summary>
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
                var promptGO = new GameObject("VoicePrompt");
                promptGO.transform.SetParent(overlayCanvas.transform, false);
                promptInstance = promptGO.AddComponent<TextMeshProUGUI>();
                promptInstance.fontSize = 28;
                promptInstance.alignment = TextAlignmentOptions.Center;
                promptInstance.color = Color.yellow;

                var rectTransform = promptInstance.rectTransform;
                rectTransform.anchorMin = new Vector2(0.5f, 0);
                rectTransform.anchorMax = new Vector2(0.5f, 0);
                rectTransform.pivot = new Vector2(0.5f, 0.5f);
                rectTransform.anchoredPosition = promptAnchoredPosition;
                rectTransform.sizeDelta = new Vector2(800, 100);
            }
        }

        string keywordsText = GeneratePromptKeywordsText();
        string action = Kind == ObstacleKind.Rocks ? "jump/over" : "duck/under";
        promptInstance.text = string.Format(promptFormat, keywordsText, action);
        promptInstance.gameObject.SetActive(true);
    }

    /// <summary>
    /// Skjuler prompten.
    /// </summary>
    private void HidePrompt()
    {
        if (promptInstance != null)
            promptInstance.gameObject.SetActive(false);
    }

    /// <summary>
    /// Genererer en kommasepareret streng med de relevante nøgleord for prompten.
    /// </summary>
    /// <returns>Streng med nøgleord eller "(no keywords)" hvis ingen er konfigureret.</returns>
    private string GeneratePromptKeywordsText()
    {
        var keywordsArray = Kind == ObstacleKind.Rocks ? rockKeywords : bambooKeywords;
        if (keywordsArray == null || keywordsArray.Length == 0) return "(no keywords)";
        return string.Join(", ", keywordsArray);
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