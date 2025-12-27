using UnityEngine;

public sealed class NormalPresenter : ISpeechTaskPresenter
{
    public void Show(SpeechTaskUI ui, string sentence) => ui.ShowTask(sentence);
}
