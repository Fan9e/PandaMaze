using UnityEngine;

public class MazeEndTrigger : MonoBehaviour
{
    /// <summary>
    /// Fyrværkeri når spilleren har gennemført labyrinten.
    /// </summary>
    public ParticleSystem fireworks; 

    /// <summary>
    /// UI-panel der vises, når spilleren har vundet,
    /// </summary>
    public GameObject winPanel;    

    /// <summary>
    /// Usynlig mur, der aktiveres når spilleren når målet,
    /// så pandaen ikke kan gå videre.
    /// </summary>
    public GameObject endBarrier;  

    /// <summary>
    /// Kaldes, når et andet collider-objekt
    /// går ind i denne triggers collider.
    /// Fyrværkeriet startes,
    /// vinder-panelet vises og den usynlige mur aktiveres.
    /// </summary>
    /// <param name="other">
    /// Det collider-objekt, der rammer triggeren,
    /// altså pandaen: ("Player").
    /// </param>
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        if (fireworks != null)
        {
            fireworks.Play();
        }

        if (winPanel != null)
            winPanel.SetActive(true);

        if (endBarrier != null)
            endBarrier.SetActive(true);
    }
}
