using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Animator))]

public class PlayerMovement : MonoBehaviour
{
    [SerializeField]
    private float movementSpeed = 5f;

    [SerializeField]
    private float inputDeadzone = 0.05f;

    private Rigidbody rigidbodyComponent;
    private Animator animator;
    private Vector3 movementInput;

    /// <summary>
    /// Henter referencer til de komponenter, som spilleren kræver for at fungere.
    /// </summary>
    private void Awake()
    {
        rigidbodyComponent = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
    }

    /// <summary>
    /// Læser input fra spilleren og opdaterer bevægelse og animationstilstand.
    /// </summary>
    private void Update()
    {
        UpdateMovementInput();
        UpdateAnimationState();
    }

    /// <summary>
    /// Kaldes i faste intervaller og anvender bevægelsen på rigidbody'en.
    /// </summary>
    private void FixedUpdate()
    {
        ApplyMovement();
    }

    /// <summary>
    /// Beregner kamera-relativt bevægelsesinput ud fra tastaturaksene.
    /// </summary>
    private void UpdateMovementInput()
    {
        Vector3 cameraForward =
            Vector3.ProjectOnPlane(Camera.main.transform.forward, Vector3.up).normalized;
        Vector3 cameraRight =
            Vector3.ProjectOnPlane(Camera.main.transform.right, Vector3.up).normalized;

        float horizontalInput = Input.GetAxisRaw("Horizontal");
        float verticalInput = Input.GetAxisRaw("Vertical");

        Vector3 rawInput = cameraRight * horizontalInput + cameraForward * verticalInput;

        if (rawInput.magnitude < inputDeadzone)
        {
            movementInput = Vector3.zero;
        }
        else
        {
            movementInput = rawInput.normalized;
        }
    }

    /// <summary>
    /// Opdaterer animatorens parametre på baggrund af nuværende bevægelse.
    /// </summary>
    private void UpdateAnimationState()
    {
        if (animator == null)
        {
            return;
        }

        bool isWalking = movementInput.sqrMagnitude > 0.0001f;
        animator.SetBool("IsWalking", isWalking);
    }

    /// <summary>
    /// Anvender den beregnede bevægelse på rigidbody'en og bevarer den vertikale hastighed.
    /// </summary>
    private void ApplyMovement()
    {
        Vector3 velocity = movementInput * movementSpeed;
        velocity.y = rigidbodyComponent.linearVelocity.y;
        rigidbodyComponent.linearVelocity = velocity;
    }
}
