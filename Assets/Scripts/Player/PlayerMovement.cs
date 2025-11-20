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
    // øverst i PlayerMovement
    private float joystickSpeedMultiplier = 0.5f;

    private Rigidbody rigidbodyComponent;
    private Animator animator;
    private Vector3 movementInput;

    // Bruges af joysticket på mobil
    private Vector2 joystickInput = Vector2.zero;

    public Rigidbody RigidbodyComponent
    {
        get => rigidbodyComponent;
        set => rigidbodyComponent = value;
    }

    public float MovementSpeed
    {
        get => movementSpeed;
        set => movementSpeed = value;
    }

    private void Awake()
    {
        if (rigidbodyComponent == null)
            rigidbodyComponent = GetComponent<Rigidbody>();

        if (animator == null)
            animator = GetComponent<Animator>();
    }

    /// <summary>
    /// Bruges af UNIT TESTEN – sæt movementInput direkte.
    /// </summary>
    public void SetMovementInput(Vector3 input)
    {
        movementInput = input;
    }

    /// <summary>
    /// Bruges af joysticket – sæt joystick-input.
    /// </summary>
    public void SetJoystickInput(Vector2 input)
    {
        joystickInput = input;
    }

    private void Update()
    {
        UpdateMovementInput();
        UpdateAnimationState();
    }

    private void FixedUpdate()
    {
        ApplyMovement();
    }

    /// <summary>
    /// Kombinerer joystick + tastatur til ét movementInput.
    /// </summary>
    private void UpdateMovementInput()
    {
        float horizontalInput = joystickInput.x;
        float verticalInput = joystickInput.y;

        // TEMP: ingen tastatur-fallback
        //if (Mathf.Abs(horizontalInput) < 0.01f && Mathf.Abs(verticalInput) < 0.01f)
        //{
        //    horizontalInput = Input.GetAxisRaw("Horizontal");
        //    verticalInput = Input.GetAxisRaw("Vertical");
        //}

        movementInput = new Vector3(horizontalInput, 0f, verticalInput);

        if (Mathf.Abs(movementInput.z) < inputDeadzone)
        {
            movementInput.z = 0f;
        }
    }

    private void UpdateAnimationState()
    {
        if (animator == null)
            return;

        Vector2 planarInput = new Vector2(movementInput.x, movementInput.z);
        bool isWalking = planarInput.sqrMagnitude > walkThreshold;

        animator.SetBool("IsWalking", isWalking);
    }

    public void ApplyMovement()
    {
        if (Mathf.Abs(movementInput.x) > 0.01f)
        {
            float turn = movementInput.x * rotationSpeed * Time.fixedDeltaTime;
            Quaternion deltaRot = Quaternion.Euler(0f, turn, 0f);
            rigidbodyComponent.MoveRotation(rigidbodyComponent.rotation * deltaRot);
        }

        Vector3 velocity = transform.forward * (movementInput.z * movementSpeed);

        Debug.Log("ApplyMovement → movementInput = " + movementInput);

        // Use Unity's standard Rigidbody API (velocity) instead of a nonstandard property.
        velocity.y = rigidbodyComponent.linearVelocity.y;
        rigidbodyComponent.linearVelocity = velocity;
    }
}