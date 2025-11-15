using UnityEngine;

/// <summary>
/// Interface for at modtage opdateringer fra stemmegenkendelse og mikrofon.
/// </summary>
public interface IVoiceObserver
{
    /// <summary>
    /// Kaldes når et delvist (mellemliggende) genkendelsesresultat er tilgængeligt.
    /// </summary>
    /// <param name="partial">Det delvise tekstresultat fra genkendelsen.</param>
    void OnPartialResult(string partial);

    /// <summary>
    /// Kaldes når et endeligt genkendelsesresultat (eller en fejlmeddelelse) er tilgængeligt.
    /// </summary>
    /// <param name="result">Det endelige tekstresultat eller en fejlbesked.</param>
    void OnResult(string result);

    /// <summary>
    /// Kaldes når mikrofonens lydniveau ændrer sig (0..1).
    /// </summary>
    /// <param name="level">Det nye lydniveau, normaliseret i intervallet 0 til 1.</param>
    void OnVoiceLevelChanged(float level);

    /// <summary>
    /// Kaldes når mikrofonens tilstand ændrer sig (tændt/slukket).
    /// </summary>
    /// <param name="isOn">Sandt hvis mikrofonen er aktiv/tændt; ellers falsk.</param>
    void OnMicrophoneStateChanged(bool isOn);
}
