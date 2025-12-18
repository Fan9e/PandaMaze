using UnityEngine;

public class MazeEndTrigger : MonoBehaviour
{
    /// <summary>
    /// Fyrværkeri når spilleren har gennemført labyrinten.
    /// </summary>
    public ParticleSystem fireworks;

    /// <summary>
    /// UI-panel der vises, når spilleren har vundet.
    /// </summary>
    public GameObject winPanel;

    /// <summary>
    /// Knap der vises, når spilleren har vundet.
    /// </summary>
    public GameObject startOverButton;

    /// <summary>
    /// Usynlig mur, der aktiveres når spilleren når målet,
    /// så pandaen ikke kan gå videre.
    /// </summary>
    public GameObject endBarrier;

    /// <summary>
    /// Sikrer at win-sekvensen kun kører én gang.
    /// </summary>
    private bool hasWon = false;

    /// <summary>
    /// Kaldes, når et andet collider-objekt går ind i denne triggers collider.
    /// Hvis det er spilleren, køres win-sekvensen én gang.
    /// </summary>
    /// <param name="other">Det collider-objekt, der rammer triggeren (spilleren).</param>
    private void OnTriggerEnter(Collider other)
    {
        if (hasWon) return;
        if (!other.CompareTag("Player")) return;

        hasWon = true;
        HandleWin();
    }

    /// <summary>
    /// Håndterer alt, der skal ske når spilleren vinder.
    /// </summary>
    private void HandleWin()
    {
        PlayFireworks();
        ShowWinUI();
        ActivateEndBarrier();
    }

    /// <summary>
    /// Starter fyrværkeri.
    /// </summary>
    private void PlayFireworks()
    {
        if (fireworks != null)
            fireworks.Play();
    }

    /// <summary>
    /// Viser win UI (panel + start forfra-knap).
    /// </summary>
    private void ShowWinUI()
    {
        if (winPanel != null)
            winPanel.SetActive(true);

        if (startOverButton != null)
            startOverButton.SetActive(true);
    }

    /// <summary>
    /// Aktiverer den usynlige mur.
    /// </summary>
    private void ActivateEndBarrier()
    {
        if (endBarrier != null)
            endBarrier.SetActive(true);
    }
}