using UnityEngine;

public sealed class OneWordScramblePresenter : ISpeechTaskPresenter
{
    public void Show(SpeechTaskUI ui, string sentence) => ui.ShowOneWordScrambledTask(sentence);
}
