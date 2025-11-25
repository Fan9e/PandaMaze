using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Håndterer hovedmenuens knapper og navigation til spil- og tutorial-scener.
/// </summary>
public class MainMenu : MonoBehaviour
{
    /// <summary>
    /// Loader næste scene i build-indekset (buildIndex + 1) for at starte spillet.
    /// </summary>
    public void PlayGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }

    /// <summary>
    /// Loader anden scene efter den nuværende (buildIndex + 2) for at starte tutorialen.
    /// </summary>
    public void PlayTutorial()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 2);
    }
}
