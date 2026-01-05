using UnityEngine;

public sealed class FullScramblePresenter : ISpeechTaskPresenter
{
    /// <summary>
    /// Viser den aktuelle tale-/sætningsopgave i brugergrænsefladen.
    /// </summary>
    /// <param name="speechTaskUI">
    /// UI-komponenten der står for at præsentere speech-tasken.
    /// </param>
    /// <param name="sentence">
    /// Sætningen der skal vises til brugeren.
    /// </param>
    public void Show(SpeechTaskUI speechTaskUI, string sentence) => speechTaskUI.ShowScrambledTask(sentence);

}
