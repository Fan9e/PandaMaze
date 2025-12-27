using UnityEngine;

public sealed class FullScramblePresenter : ISpeechTaskPresenter
{
    public void Show(SpeechTaskUI ui, string sentence) => ui.ShowScrambledTask(sentence);

}
