using UnityEngine;

public class CatSpeechCombat : MonsterSpeechCombatBase
{
    /// <summary>
    /// Viser sætningen med én-ord-scrambling (kun ét ord flyttes til en forkert position).
    /// Hvis sætningen er tom, faldes der tilbage til en normal opgave.
    /// Dette er kattens særlige opgaveform, som udfordrer spilleren til
    /// at rette sætningen i hovedet og sige den korrekt.
    /// </summary>
    /// <param name="sentence">Den korrekte sætning, der skal forvrænges.</param>
    protected override void ShowSentenceWithCorrectMode(string sentence)
    {
        if (!string.IsNullOrEmpty(sentence))
        {
            speechTaskUI.ShowOneWordScrambledTask(sentence);
        }
        else
        {
            speechTaskUI.ShowTask();
        }
    }
}
