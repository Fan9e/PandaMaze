using UnityEngine;

public class DragonSpeechCombat : MonsterSpeechCombatBase
{
    /// <summary>
    /// Viser sætningen i dens normale form (ingen scrambling).
    /// Dette er dragens unikke kampform, hvor barnet blot skal gentage
    /// sætningen præcist for at skade monsteret.
    /// </summary>
    /// <param name="sentence">Den korrekte sætning, der skal vises.</param>
    protected override void ShowSentenceWithCorrectMode(string sentence)
    {
        speechTaskUI.ShowTask(sentence);
    }
}
