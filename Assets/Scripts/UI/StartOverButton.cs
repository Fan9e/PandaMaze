using UnityEngine;
using UnityEngine.SceneManagement;

public class StartOverButton : MonoBehaviour
{
    /// <summary>
    /// Loader den nuværende scene igen, så spillet starter forfra.
    /// </summary>
    public void RestartGame()
    {
        Time.timeScale = 1f;

        var currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.buildIndex);
    }
}
