using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// En port/obstacle der reagerer på talte kommandoer for at lade spilleren passere.
/// Registrerer sig som en IVoiceObserver når spilleren går ind i triggeren,
/// viser relevante nøgleord i UI og udfører enten en "over" eller "under" passage.
/// </summary>
public class ObstacleVoiceGate : MonoBehaviour, IVoiceObserver
{
    /// <summary>
    /// Type af forhindring - afgør hvilke kommandoer der tæller som "over" eller "under".
    /// </summary>
    public enum ObstacleKind { Rocks, FallenBamboo }

    [Header("General")]
    /// <summary>
    /// Hvilken slags forhindring dette objekt repræsenterer.
    /// </summary>
    public ObstacleKind Kind = ObstacleKind.Rocks;

    [Tooltip("If true the script will gather child colliders and forward their trigger events to this component.")]
    /// <summary>
    /// Hvis sandt, vil komponenten samle kollidere fra børn og videresende deres trigger-events hertil.
    /// </summary>
    public bool useChildColliders = true;

    [Header("Pass settings")]
    /// <summary>
    /// Hvor langt spilleren bevæger sig frem under passagen (måleenhed: Unity units/meters).
    /// </summary>
    public float passDistance = 3.5f;
    /// <summary>
    /// Varighed i sekunder for passagens animation (over/under).
    /// </summary>
    public float passDuration = 0.5f;
    /// <summary>
    /// Maksimum højde der løftes ved en "over"-passage.
    /// </summary>
    public float overHeight = 1.5f;
    /// <summary>
    /// Maksimum dybde der dykkes ved en "under"-passage.
    /// </summary>
    public float underDepth = 0.5f;
    [Tooltip("If true will attempt to start the microphone automatically when the player enters.")]
    /// <summary>
    /// Hvis sandt forsøger VoiceMovement at starte mikrofonen automatisk når spilleren går ind.
    /// </summary>
    public bool autoStartMicrophone = true;

    [Header("Collider Ducking (optional)")]
    [Tooltip("If true will temporarily reduce player's CapsuleCollider/CharacterController height while performing an 'under' pass.")]
    /// <summary>
    /// Hvis sandt ændres spillerens CapsuleCollider/CharacterController højde midlertidigt under en "under"-passage.
    /// </summary>
    public bool changePlayerColliderWhenDucking = true;
    [Tooltip("Multiplier applied to collider height when ducking (0..1). Typical 0.4-0.7.")]
    /// <summary>
    /// Multiplikator der anvendes på collider-højden ved ducking (0..1).
    /// </summary>
    public float duckColliderHeightMultiplier = 0.5f;
    [Tooltip("Duration (seconds) for the collider height change to lerp down/up.")]
    /// <summary>
    /// Hvor lang tid (sekunder) det tager at lerpe collider-højden ned/op igen.
    /// </summary>
    public float colliderChangeDuration = 0.12f;

    [Header("Keywords (editable)")]
    /// <summary>
    /// Liste af nøgleord der tæller som "over"-kommandoer (for Rocks).
    /// </summary>
    public string[] rockKeywords = new[] { "gå  over", "hop over", "hop op" };
    /// <summary>
    /// Liste af nøgleord der tæller som "under"-kommandoer (for FallenBamboo).
    /// </summary>
    public string[] bambooKeywords = new[] { "duk under", "gå under", "ned under" };

    [Tooltip("If true the script will try to make concave MeshColliders convex when creating child forwarders. Use with care.")]
    /// <summary>
    /// Hvis sandt forsøger scriptet at gøre konvekse MeshColliders til konvekse når der oprettes child forwarders.
    /// Brug med omhu - konveksions kan ændre kollisionsform og kan fejle for komplekse meshes.
    /// </summary>
    public bool autoMakeMeshCollidersConvex = false;

    /// <summary>
    /// Om spilleren i øjeblikket befinder sig inde i gate-triggeren.
    /// </summary>
    private bool playerInside = false;
    /// <summary>
    /// Cache reference til spillerens <see cref="Transform"/> mens spilleren er inde i triggeren.
    /// </summary>
    private Transform playerTransform;
    /// <summary>
    /// Cache reference til spillerens <see cref="Rigidbody"/> for at kunne sætte kinematic under passager.
    /// </summary>
    private Rigidbody playerRigidbody;
    /// <summary>
    /// Cache reference til spillerens <see cref="PlayerMovement"/> så movement kan disable/enable.
    /// </summary>
    private PlayerMovement playerMovement;
    /// <summary>
    /// Cache reference til <see cref="VoiceMovement"/> brugt til at registrere/afregistrere som observer.
    /// </summary>
    private VoiceMovement voiceMovement;
    /// <summary>
    /// Indikerer om en passage-animation (over/under) i øjeblikket kører.
    /// </summary>
    private bool isPassing = false;

    /// <summary>
    /// Lower-cased cache af rock-nøgleord til hurtig sammenligning.
    /// </summary>
    private string[] rockKeywordsLower;
    /// <summary>
    /// Lower-cased cache af bamboo-nøgleord til hurtig sammenligning.
    /// </summary>
    private string[] bambooKeywordsLower;

    /// <summary>
    /// Faktor der normaliserer parabelformen progress*(1-progress) så toppen = 1 ved progress=0.5.
    /// Bruges sammen med overHeight/underDepth til at styre maksimal højde/dybde.
    /// </summary>
    private const float ParabolaNormalization = 4f;

    private CapsuleCollider duckCapsuleCollider;
    private CharacterController duckCharacterController;
    private float duckOriginalHeight = 0f;
    private Vector3 duckOriginalCenter = Vector3.zero;
    private float duckTargetHeight = 0f;
    private Vector3 duckTargetCenter = Vector3.zero;

    /// <summary>
    /// Unity callback kørt i editor/inspektør når værdier ændres. Sørger for at oprette child-forwarders
    /// og opdatere cache af nøgleord.
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

        UpdateKeywordCache();
    }

    /// <summary>
    /// Awake initialisering: sætter egen collider til trigger hvis mulig, sikrer child-forwarders og opdaterer cache.
    /// </summary>
    private void Awake()
    {
        var ownCollider = GetComponent<Collider>();
        if (ownCollider != null)
            TrySetTriggerSafe(ownCollider);

        if (useChildColliders)
            EnsureChildForwarders(useUndo: false);

        UpdateKeywordCache();
    }

    /// <summary>
    /// Opdaterer interne lower-case caches for nøgleord, for hurtigere sammenligning ved talegenkendelse.
    /// </summary>
    private void UpdateKeywordCache()
    {
        rockKeywordsLower = (rockKeywords ?? System.Array.Empty<string>())
            .Where(keyword => !string.IsNullOrEmpty(keyword))
            .Select(keyword => keyword.ToLowerInvariant())
            .ToArray();

        bambooKeywordsLower = (bambooKeywords ?? System.Array.Empty<string>())
            .Where(keyword => !string.IsNullOrEmpty(keyword))
            .Select(keyword => keyword.ToLowerInvariant())
            .ToArray();
    }

    /// <summary>
    /// Søger efter colliders i børnenoder og tilføjer/enabler et ObstacleTriggerForwarder-komponent der videresender trigger-events.
    /// </summary>
    /// <param name="useUndo">Hvis sandt bruges Undo når komponenter oprettes (kun i editor).</param>
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
    /// Forsøger sikkert at sætte en collider som trigger. Springes over for ikke-konvekse MeshColliders.
    /// </summary>
    /// <param name="colliderToSet">Collideren der skal sættes som trigger.</param>
    /// <returns>True hvis operationen lykkedes og collideren er/is trigger.</returns>
    private bool TrySetTriggerSafe(Collider colliderToSet)
    {
        if (colliderToSet == null) return false;

        if (colliderToSet is MeshCollider meshCollider && !meshCollider.convex)
        {
            if (autoMakeMeshCollidersConvex)
            {
#if UNITY_EDITOR
                if (!EditorApplication.isPlaying)
                {
                    Undo.RecordObject(meshCollider, "Make MeshCollider convex and set trigger");
                    meshCollider.convex = true;
                    colliderToSet.isTrigger = true;
                }
                else
                {
                    meshCollider.convex = true;
                    colliderToSet.isTrigger = true;
                }
#else
                meshCollider.convex = true;
                colliderToSet.isTrigger = true;
#endif
                Debug.Log($"ObstacleVoiceGate: Converted MeshCollider '{colliderToSet.name}' to convex and set as trigger.", colliderToSet.gameObject);
                return true;
            }

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
    /// Modtager forwardede OnTriggerEnter-events fra child-colliders.
    /// </summary>
    internal void ChildTriggerEnter(GameObject childColliderOwner, Collider other) => HandleTriggerEnter(other);
    /// <summary>
    /// Modtager forwardede OnTriggerExit-events fra child-colliders.
    /// </summary>
    internal void ChildTriggerExit(GameObject childColliderOwner, Collider other) => HandleTriggerExit(other);

    private void OnTriggerEnter(Collider other) => HandleTriggerEnter(other);
    private void OnTriggerExit(Collider other) => HandleTriggerExit(other);

    /// <summary>
    /// Håndterer når noget går ind i triggeren. Hvis det er spilleren registreres voice observer og UI viser nøgleord.
    /// </summary>
    /// <param name="other">Collideren som gik ind i triggeren.</param>
    private void HandleTriggerEnter(Collider other)
    {
        if (playerInside) return;
        if (!TryGetPlayerFromCollider(other, out var foundPlayerMovement, out var foundRigidbody)) return;

        playerInside = true;
        playerMovement = foundPlayerMovement;
        playerTransform = foundPlayerMovement.transform;
        playerRigidbody = foundRigidbody;

        RegisterVoiceObserver();

        var keywords = GetKeywordsForKind();
        ObstacleKeywordsUI.Instance?.ShowKeywords(keywords);

        Debug.Log($"ObstacleVoiceGate: Player entered {name}. Say the correct command to pass ({Kind}).");
    }

    /// <summary>
    /// Håndterer når noget forlader triggeren. Hvis det er spilleren afregistreres voice observer og UI skjules.
    /// </summary>
    /// <param name="other">Collideren som forlod triggeren.</param>
    private void HandleTriggerExit(Collider other)
    {
        if (!TryGetPlayerFromCollider(other, out var foundPlayerMovement, out var foundRigidbody)) return;
        if (!playerInside) return;

        playerInside = false;
        UnregisterVoiceObserver();

        ObstacleKeywordsUI.Instance?.HideKeywords();

        if (!isPassing)
            ClearPlayerReferences();

        Debug.Log($"ObstacleVoiceGate: Player left {name}. Voice observer unregistered.");
    }

    /// <summary>
    /// Rydder gemte referencer til spilleren.
    /// </summary>
    private void ClearPlayerReferences()
    {
        playerTransform = null;
        playerRigidbody = null;
        playerMovement = null;
    }

    /// <summary>
    /// Forsøger at finde PlayerMovement og Rigidbody ud fra en collider.
    /// </summary>
    /// <param name="collider">Collider at søge fra.</param>
    /// <param name="foundPlayerMovement">Returnerer fundet PlayerMovement hvis succes.</param>
    /// <param name="foundRigidbody">Returnerer fundet Rigidbody hvis succes.</param>
    /// <returns>True hvis en spiller blev fundet.</returns>
    private bool TryGetPlayerFromCollider(Collider collider, out PlayerMovement foundPlayerMovement, out Rigidbody foundRigidbody)
    {
        foundPlayerMovement = collider.GetComponentInParent<PlayerMovement>();
        if (foundPlayerMovement == null)
        {
            foundRigidbody = null;
            return false;
        }

        foundRigidbody = foundPlayerMovement.GetComponent<Rigidbody>();
        return true;
    }

    /// <summary>
    /// Henter de relevante nøgleord baseret på ObstacleKind.
    /// </summary>
    /// <returns>Array af nøgleord som skal vises/tjekkes.</returns>
    private string[] GetKeywordsForKind()
    {
        return Kind == ObstacleKind.Rocks ? rockKeywords : bambooKeywords;
    }

    /// <summary>
    /// Registrerer denne gate som en observer på VoiceMovement (hvis tilgængelig) og starter mikrofon automatisk hvis sat.
    /// </summary>
    private void RegisterVoiceObserver()
    {
        if (voiceMovement == null)
            voiceMovement = FindObjectOfType<VoiceMovement>();

        if (voiceMovement == null) return;

        voiceMovement.RegisterObserver(this);
        if (autoStartMicrophone)
            voiceMovement.StartMicrophone();
    }

    /// <summary>
    /// Afregistrerer denne gate som observer og nulstiller lokal voiceMovement-referencen.
    /// </summary>
    private void UnregisterVoiceObserver()
    {
        if (voiceMovement != null)
            voiceMovement.UnregisterObserver(this);
        voiceMovement = null;
    }

    #region IVoiceObserver
    /// <summary>
    /// Delvis tale-resultat (kan ignoreres).
    /// </summary>
    /// <param name="partial">Den delvise tekst som blev genkendt.</param>
    public void OnPartialResult(string partial) { }

    /// <summary>
    /// Hovedmetode der modtager ferdigt genkendt tale. Matcher mod nøgleord og starter passende passage.
    /// </summary>
    /// <param name="result">Det genkendte tale-resultat som tekst.</param>
    public void OnResult(string result)
    {
        if (!playerInside || isPassing) return;

        string spoken = (result ?? string.Empty).ToLowerInvariant().Trim();
        if (string.IsNullOrEmpty(spoken)) return;

        if (MatchesKeywords(spoken, out var isOverAction))
        {
            ObstacleKeywordsUI.Instance?.HideKeywords();

            if (isOverAction)
                StartCoroutine(PerformOver());
            else
                StartCoroutine(PerformUnder());
        }
        else
        {
            Debug.Log($"ObstacleVoiceGate: command not recognized for {Kind}. Spoken: '{result}'");
        }

        UnregisterVoiceObserver();
    }

    /// <summary>
    /// Modtages når stemmeniveauet ændres (kan ignoreres).
    /// </summary>
    /// <param name="level">Styrken af mikrofon-input.</param>
    public void OnVoiceLevelChanged(float level) { }

    /// <summary>
    /// Modtages når mikrofonens tilstand ændres (kan ignoreres).
    /// </summary>
    /// <param name="isOn">Om mikrofonen er tændt.</param>
    public void OnMicrophoneStateChanged(bool isOn) { }
    #endregion

    /// <summary>
    /// Sammenligner det talte input med cachede nøgleord for den aktuelle ObstacleKind.
    /// </summary>
    /// <param name="spoken">Det talte og normaliserede tekstinput.</param>
    /// <param name="isOverAction">Returneres som true hvis det matchede et "over"-ord.</param>
    /// <returns>True hvis et nøgleord matchede.</returns>
    private bool MatchesKeywords(string spoken, out bool isOverAction)
    {
        isOverAction = false;
        if (Kind == ObstacleKind.Rocks)
        {
            if (rockKeywordsLower != null && rockKeywordsLower.Any(keyword => spoken.Contains(keyword)))
            {
                isOverAction = true;
                return true;
            }
        }
        else
        {
            if (bambooKeywordsLower != null && bambooKeywordsLower.Any(keyword => spoken.Contains(keyword)))
            {
                isOverAction = false;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Sætter spillerens movement og rigidbody i en 'paused' tilstand før passagen (disables movement, sætter kinematic).
    /// </summary>
    /// <param name="previousKinematicState">Returnerer rigidbody's tidligere kinematic-tilstand så den kan gendannes.</param>
    private void PausePlayerForPass(out bool previousKinematicState)
    {
        previousKinematicState = false;
        if (playerMovement != null)
            playerMovement.enabled = false;

        if (playerRigidbody != null)
        {
            previousKinematicState = playerRigidbody.isKinematic;
            playerRigidbody.isKinematic = true;
        }
    }

    /// <summary>
    /// Gendanner spillerens movement og rigidbody-tilstand efter passagen.
    /// </summary>
    /// <param name="previousKinematicState">Den tidligere kinematic-tilstand der skal gendannes.</param>
    private void ResumePlayerAfterPass(bool previousKinematicState)
    {
        if (playerRigidbody != null)
            playerRigidbody.isKinematic = previousKinematicState;

        if (playerMovement != null)
            playerMovement.enabled = true;
    }

    /// <summary>
    /// Coroutine der udfører en "over"-passage: løfter spilleren i en kort bue fremad.
    /// </summary>
    private IEnumerator PerformOver()
    {
        if (playerTransform == null || playerRigidbody == null || playerMovement == null) yield break;
        isPassing = true;

        PausePlayerForPass(out var previousKinematicState);

        Vector3 start = playerTransform.position;
        Vector3 forward = playerTransform.forward.normalized;
        Vector3 end = start + forward * passDistance;
        float elapsed = 0f;

        while (elapsed < passDuration)
        {
            float progress = Mathf.Clamp01(elapsed / passDuration);
            Vector3 horiz = Vector3.Lerp(start, end, progress);
            float height = ParabolaNormalization * overHeight * progress * (1 - progress);
            playerTransform.position = new Vector3(horiz.x, horiz.y + height, horiz.z);
            elapsed += Time.deltaTime;
            yield return null;
        }

        playerTransform.position = end;
        ResumePlayerAfterPass(previousKinematicState);
        isPassing = false;
    }

    /// <summary>
    /// Coroutine der udfører en "under"-passage: dipper spilleren nedad og valgfrit smalner collideren ind under passagen.
    /// </summary>
    private IEnumerator PerformUnder()
    {
        if (playerTransform == null || playerRigidbody == null || playerMovement == null) yield break;
        isPassing = true;

        PausePlayerForPass(out var previousKinematicState);


        SetupDuckCollider();

        Vector3 start = playerTransform.position;
        Vector3 forward = playerTransform.forward.normalized;
        Vector3 end = start + forward * passDistance;
        float elapsed = 0f;

        while (elapsed < passDuration)
        {
            float progress = Mathf.Clamp01(elapsed / passDuration);
            Vector3 horiz = Vector3.Lerp(start, end, progress);
            float dip = -ParabolaNormalization * underDepth * progress * (1 - progress);
            playerTransform.position = new Vector3(horiz.x, horiz.y + dip, horiz.z);

            UpdateDuckColliderDuringMove(elapsed);

            elapsed += Time.deltaTime;
            yield return null;
        }

        playerTransform.position = end;

        if (changePlayerColliderWhenDucking && (duckCapsuleCollider != null || duckCharacterController != null))
        {
            yield return RestoreDuckCollider();
        }

        ResumePlayerAfterPass(previousKinematicState);
        isPassing = false;

        if (!playerInside)
            ClearPlayerReferences();
    }

    /// <summary>
    /// Forbereder collider-referencer og beregner originale og målværdier til duk.
    /// </summary>
    private void SetupDuckCollider()
    {
        duckCapsuleCollider = null;
        duckCharacterController = null;
        duckOriginalHeight = 0f;
        duckOriginalCenter = Vector3.zero;
        duckTargetHeight = 0f;
        duckTargetCenter = Vector3.zero;

        if (!changePlayerColliderWhenDucking || playerTransform == null)
            return;

        duckCapsuleCollider = playerTransform.GetComponent<CapsuleCollider>() ?? playerTransform.GetComponentInChildren<CapsuleCollider>();
        duckCharacterController = playerTransform.GetComponent<CharacterController>() ?? playerTransform.GetComponentInChildren<CharacterController>();

        if (duckCapsuleCollider != null)
        {
            duckOriginalHeight = duckCapsuleCollider.height;
            duckOriginalCenter = duckCapsuleCollider.center;
            duckTargetHeight = Mathf.Max(0.01f, duckOriginalHeight * duckColliderHeightMultiplier);
            float delta = (duckOriginalHeight - duckTargetHeight) * 0.5f;
            duckTargetCenter = duckOriginalCenter - new Vector3(0f, delta, 0f);
        }
        else if (duckCharacterController != null)
        {
            duckOriginalHeight = duckCharacterController.height;
            duckOriginalCenter = duckCharacterController.center;
            duckTargetHeight = Mathf.Max(0.01f, duckOriginalHeight * duckColliderHeightMultiplier);
            float delta = (duckOriginalHeight - duckTargetHeight) * 0.5f;
            duckTargetCenter = duckOriginalCenter - new Vector3(0f, delta, 0f);
        }
    }

    /// <summary>
    /// Opdaterer spillerens collider under bevægelsen (lerper højden ned over colliderChangeDuration).
    /// </summary>
    /// <param name="elapsed">Tid gået siden passagens start i sekunder.</param>
    private void UpdateDuckColliderDuringMove(float elapsed)
    {
        if (!changePlayerColliderWhenDucking) return;
        if (duckCapsuleCollider == null && duckCharacterController == null) return;

        float colliderLerpT = Mathf.Clamp01(elapsed / colliderChangeDuration);

        if (duckCapsuleCollider != null)
        {
            duckCapsuleCollider.height = Mathf.Lerp(duckOriginalHeight, duckTargetHeight, colliderLerpT);
            duckCapsuleCollider.center = Vector3.Lerp(duckOriginalCenter, duckTargetCenter, colliderLerpT);
        }
        else if (duckCharacterController != null)
        {
            duckCharacterController.height = Mathf.Lerp(duckOriginalHeight, duckTargetHeight, colliderLerpT);
            duckCharacterController.center = Vector3.Lerp(duckOriginalCenter, duckTargetCenter, colliderLerpT);
        }
    }

    /// <summary>
    /// Gendanner spillerens collider til oprindelige værdier over colliderChangeDuration.
    /// </summary>
    private IEnumerator RestoreDuckCollider()
    {
        if (!changePlayerColliderWhenDucking) yield break;
        if (duckCapsuleCollider == null && duckCharacterController == null) yield break;

        float restoreElapsed = 0f;
        while (restoreElapsed < colliderChangeDuration)
        {
            float restoreT = Mathf.Clamp01(restoreElapsed / colliderChangeDuration);

            if (duckCapsuleCollider != null)
            {
                duckCapsuleCollider.height = Mathf.Lerp(duckTargetHeight, duckOriginalHeight, restoreT);
                duckCapsuleCollider.center = Vector3.Lerp(duckTargetCenter, duckOriginalCenter, restoreT);
            }
            else if (duckCharacterController != null)
            {
                duckCharacterController.height = Mathf.Lerp(duckTargetHeight, duckOriginalHeight, restoreT);
                duckCharacterController.center = Vector3.Lerp(duckTargetCenter, duckOriginalCenter, restoreT);
            }

            restoreElapsed += Time.deltaTime;
            yield return null;
        }

        if (duckCapsuleCollider != null)
        {
            duckCapsuleCollider.height = duckOriginalHeight;
            duckCapsuleCollider.center = duckOriginalCenter;
        }
        else if (duckCharacterController != null)
        {
            duckCharacterController.height = duckOriginalHeight;
            duckCharacterController.center = duckOriginalCenter;
        }

        yield break;
    }

    [DisallowMultipleComponent]
    /// <summary>
    /// Liten helper-komponent der placeres på child-collider gameobjects for at videresende OnTrigger events til parent-gaten.
    /// </summary>
    private sealed class ObstacleTriggerForwarder : MonoBehaviour
    {
        /// <summary>
        /// Reference til den parent ObstacleVoiceGate som skal modtage forwarded events.
        /// </summary>
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