using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Animator))]

public class PlayerMovement : MonoBehaviour
{
    [SerializeField]
    private float movementSpeed = 5f;
    private float walkThreshold = 0.0001f;
    private float inputDeadzone = 0.05f;
    private float rotationSpeed = 20f;

    private Rigidbody rigidbodyComponent;
    private Animator animator;
    private Vector3 movementInput;

    /// <summary>
    /// kan sætte rigidbody direkte
    /// </summary>
    public Rigidbody RigidbodyComponent
    {
        get => rigidbodyComponent;
        set => rigidbodyComponent = value;
    }

    /// <summary>
    /// Hjælpe-property så den kan styre farten
    /// </summary>
    public float MovementSpeed
    {
        get => movementSpeed;
        set => movementSpeed = value;
    }

    /// <summary>
    /// Henter referencer til de komponenter, som spilleren kræver for at fungere.
    /// </summary>
    private void Awake()
    {
        if (rigidbodyComponent == null)
            rigidbodyComponent = GetComponent<Rigidbody>();

        if (animator == null)
            animator = GetComponent<Animator>();
    }

    /// <summary>
    /// Hjælpemetode til, så vi kan sætte movementInput direkte
    /// </summary>
    public void SetMovementInput(Vector3 input)
    {
        movementInput = input;
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
        float horizontalInput = Input.GetAxisRaw("Horizontal"); // venstre/højre pil
        float verticalInput = Input.GetAxisRaw("Vertical");   // op/ned pil

        movementInput = new Vector3(horizontalInput, 0f, verticalInput);

        if (Mathf.Abs(movementInput.z) < inputDeadzone)
        {
            movementInput.z = 0f;
        }
    }

    /// <summary>
    /// Opdaterer animatoren: går kun når vi bevæger os frem/bagud.
    /// </summary>
    private void UpdateAnimationState()
    {
        if (animator == null)
            return;

        // Kig på både rotation (x) og frem/bagud (z)
        Vector2 planarInput = new Vector2(movementInput.x, movementInput.z);
        bool isWalking = planarInput.sqrMagnitude > walkThreshold;

        animator.SetBool("IsWalking", isWalking);
    }


    /// <summary>
    /// Roterer pandaen med Horizontal og bevæger den frem/bagud med Vertical.
    /// </summary>
    public void ApplyMovement()
    {
        // 1) Drej pandaen rundt om Y-aksen ud fra Horizontal input
        if (Mathf.Abs(movementInput.x) > 0.01f)
        {
            float turn = movementInput.x * rotationSpeed * Time.fixedDeltaTime;
            Quaternion deltaRot = Quaternion.Euler(0f, turn, 0f);
            rigidbodyComponent.MoveRotation(rigidbodyComponent.rotation * deltaRot);
        }

        // 2) Gå frem/bagud i den retning pandaen peger (transform.forward)
        Vector3 velocity = transform.forward * (movementInput.z * movementSpeed);
        velocity.y = rigidbodyComponent.linearVelocity.y; // behold hop/tyngdekraft
        rigidbodyComponent.linearVelocity = velocity;
    }
}
