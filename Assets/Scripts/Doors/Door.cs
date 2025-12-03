using UnityEngine;

/// <summary>
/// En låst dør, som kræver en bestemt nøgle fra spillerens inventory.
/// </summary>
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

    private void Awake()
    {
        _collider = GetComponent<Collider>();
        if (doorVisual == null)
            doorVisual = gameObject;
    }

    private void OnCollisionEnter(Collision collision)
    {
        Debug.Log("Panda ramte en dør: " + gameObject.name);

        if (!collision.collider.CompareTag("Player"))
            return;

        var inventory = collision.collider.GetComponent<PlayerInventory>();
        if (inventory == null)
            return;

        if (inventory.HasKey(doorId))
        {
            OpenDoor();
        }
        else
        {
            Debug.Log("Døren er låst. Du mangler den rigtige nøgle.");

            if (UIMessageManager.Instance != null)
            {
                UIMessageManager.Instance.ShowMessage(
                    "Du mangler den rigtige nøgle til denne dør.",
                    5f
                );
            }
            else
            {
                Debug.LogWarning("UIMessageManager.Instance er NULL");
            }
        }
    }

    private void OpenDoor()
    {
        if (_collider != null)
            _collider.enabled = false;

        if (doorVisual != null)
            doorVisual.SetActive(false);

        Debug.Log($"Dør {doorId} er nu åben.");
    }
}
