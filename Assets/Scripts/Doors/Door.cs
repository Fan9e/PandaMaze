using UnityEngine;

[RequireComponent(typeof(Collider))]


public class Door : MonoBehaviour
{
    [SerializeField]
    [Tooltip("ID på denne dør. Skal matche nøglens ID.")]
    private int doorId = 1;

    [SerializeField]
    [Tooltip("Objektet der skal skjules når døren åbner. Hvis tomt, bruges hele GameObjectet.")]
    private GameObject doorVisual;

    private Collider _collider;

    /// <summary>
    /// Initialiserer dørens komponenter.
    /// Finder dørens collider og sikrer, at doorVisual peger på det rigtige objekt.
    /// </summary>
    private void Awake()
    {
        _collider = GetComponent<Collider>();
        if (doorVisual == null)
            doorVisual = gameObject;
    }

    /// <summary>
    /// Håndterer, når spilleren går ind i døren.
    /// Tjekker om spilleren har den korrekte nøgle:
    /// - Hvis ja: åbnes døren.
    /// - Hvis nej: vises en besked om, at nøglen mangler.
    /// </summary>
    /// <param name="collision">Det objekt, der kolliderer med døren.</param>
    private void OnCollisionEnter(Collision collision)
    {
        const float MessageDuration = 5f;

        Debug.Log("Panda ramte en dør: " + gameObject.name);

        // Kun spilleren må interagere med døren
        if (!collision.collider.CompareTag("Player"))
            return;

        // Hent spillerens inventory
        var inventory = collision.collider.GetComponent<PlayerInventory>();
        if (inventory == null)
            return;

        // Har spilleren den rigtige nøgle?
        if (inventory.HasKey(doorId))
        {
            OpenDoor();
        }
        else
        {
            Debug.Log("Døren er låst. Du mangler den rigtige nøgle.");

            // Vis UI-besked hvis manageren findes
            if (UIMessageManager.Instance != null)
            {
                UIMessageManager.Instance.ShowMessage(
                    "Du mangler den rigtige nøgle til denne dør.",
                    MessageDuration
                );
            }
            else
            {
                Debug.LogWarning("UIMessageManager.Instance er NULL");
            }
        }
    }

    /// <summary>
    /// Åbner døren ved at deaktivere dens collider og skjule dens visuelle objekt.
    /// Kaldet når spilleren har den korrekte nøgle.
    /// </summary>
    private void OpenDoor()
    {
        if (_collider != null)
            _collider.enabled = false;

        if (doorVisual != null)
            doorVisual.SetActive(false);

        Debug.Log($"Dør {doorId} er nu åben.");
    }
}
