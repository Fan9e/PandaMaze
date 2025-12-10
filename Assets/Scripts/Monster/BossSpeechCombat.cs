using UnityEngine;

public class BossSpeechCombat : MonsterSpeechCombatBase
{
    /// <summary>
    /// Viser bossens opgave som en fuld-scramble, hvor alle ordene i sætningen
    /// kan blive byttet rundt. Dette giver den højeste sværhedsgrad og kræver,
    /// at spilleren genopbygger hele sætningen ud fra hukommelse.
    /// Hvis der mangler en sætning, faldes der tilbage til normal opgavevisning.
    /// </summary>
    /// <param name="sentence">Den korrekte sætning, som skal scrambled fuldstændigt.</param>
    protected override void ShowSentenceWithCorrectMode(string sentence)
    {
        if (!string.IsNullOrEmpty(sentence))
        {
            speechTaskUI.ShowScrambledTask(sentence);
        }
        else
        {
            speechTaskUI.ShowTask();
        }
    }
}
