using UnityEngine;

public class MazeEndTrigger : MonoBehaviour
{
    public ParticleSystem fireworks;  // Dit fyrværkeri
    public GameObject winPanel;       // UI-panel med "Yaaay" (kan være tom)
    public GameObject endBarrier;     // Den usynlige mur

    private void OnTriggerEnter(Collider other)
    {
        // Sørg for at pandaen har tag "Player"
        if (!other.CompareTag("Player")) return;

        // Tænd fyrværkeri
        if (fireworks != null)
        {
            fireworks.Play();
        }

        // Vis "du har vundet"-tekst (hvis du har sat en)
        if (winPanel != null)
            winPanel.SetActive(true);

        // Tænd usynlig mur
        if (endBarrier != null)
            endBarrier.SetActive(true);
    }
}
