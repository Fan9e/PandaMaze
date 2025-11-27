//using UnityEngine;
//using UnityEngine.InputSystem;

//[RequireComponent(typeof(Rigidbody))]
//[RequireComponent(typeof(Animator))]

//public class PlayerMovement : MonoBehaviour
//{
//    [SerializeField]
//    private float movementSpeed = 5f;
//    private float walkThreshold = 0.0001f;
//    private float inputDeadzone = 0.05f;
//    private float rotationSpeed = 20f;

//    private Rigidbody rigidbodyComponent;
//    private Animator animator;
//    private Vector3 movementInput;

//    // If input comes via the Input System action callback we set this to true
//    // so the old Input.GetAxis fallback does not override it.
//    private bool hasInputFromActions = false;

//    /// <summary>
//    /// kan sætte rigidbody direkte
//    /// </summary>
//    public Rigidbody RigidbodyComponent
//    {
//        get => rigidbodyComponent;
//        set => rigidbodyComponent = value;
//    }

//    /// <summary>
//    /// Hjælpe-property så den kan styre farten
//    /// </summary>
//    public float MovementSpeed
//    {
//        get => movementSpeed;
//        set => movementSpeed = value;
//    }

//    /// <summary>
//    /// Henter referencer til de komponenter, som spilleren kræver for at fungere.
//    /// </summary>
//    private void Awake()
//    {
//        if (rigidbodyComponent == null)
//            rigidbodyComponent = GetComponent<Rigidbody>();

//        if (animator == null)
//            animator = GetComponent<Animator>();
//    }

//    /// <summary>
//    /// Hjælpemetode til, så vi kan sætte movementInput direkte
//    /// </summary>
//    public void SetMovementInput(Vector3 input)
//    {
//        movementInput = input;
//        hasInputFromActions = true;
//    }

//    /// <summary>
//    /// Input System callback. If you are using PlayerInput with "Send Messages"
//    /// and your action is named "MoveAndTurn" this method will be called
//    /// automatically as OnMoveAndTurn(InputValue).
//    /// </summary>

//public void OnMoveAndTurn(InputAction.CallbackContext context)
//{
//    Vector2 v = context.ReadValue<Vector2>();

//    // apply deadzone
//    if (Mathf.Abs(v.x) < inputDeadzone) v.x = 0f;
//    if (Mathf.Abs(v.y) < inputDeadzone) v.y = 0f;

//    movementInput = new Vector3(v.x, 0f, v.y);
//    hasInputFromActions = true;
//}

///// <summary>
///// Læser input fra spilleren og opdaterer bevægelse og animationstilstand.
///// </summary>
//private void Update()
//    {
//        UpdateMovementInput();
//        UpdateAnimationState();

//        // reset the flag each frame so keyboard/other fallback can be used
//        // if no action callback is received next frame.
//        hasInputFromActions = false;
//    }

//    /// <summary>
//    /// Kaldes i faste intervaller og anvender bevægelsen på rigidbody'en.
//    /// </summary>
//    private void FixedUpdate()
//    {
//        ApplyMovement();
//    }

//    /// <summary>
//    /// Beregner kamera-relativt bevægelsesinput ud fra tastaturaksene.
//    /// If an Input System action provided input this frame we skip the fallback.
//    /// </summary>
//    private void UpdateMovementInput()
//    {
//        if (hasInputFromActions)
//            return;

//        float horizontalInput = Input.GetAxisRaw("Horizontal"); // venstre/højre pil
//        float verticalInput = Input.GetAxisRaw("Vertical");   // op/ned pil

//        movementInput = new Vector3(horizontalInput, 0f, verticalInput);

//        if (Mathf.Abs(movementInput.z) < inputDeadzone)
//        {
//            movementInput.z = 0f;
//        }
//    }

//    /// <summary>
//    /// Opdaterer animatoren: går kun når vi bevæger os frem/bagud.
//    /// </summary>
//    private void UpdateAnimationState()
//    {
//        if (animator == null)
//            return;

//        // Kig på både rotation (x) og frem/bagud (z)
//        Vector2 planarInput = new Vector2(movementInput.x, movementInput.z);
//        bool isWalking = planarInput.sqrMagnitude > walkThreshold;

//        animator.SetBool("IsWalking", isWalking);
//    }


//    /// <summary>
//    /// Roterer pandaen med Horizontal og bevæger den frem/bagud med Vertical.
//    /// </summary>
//    public void ApplyMovement()
//    {
//        // 1) Drej pandaen rundt om Y-aksen ud fra Horizontal input
//        if (Mathf.Abs(movementInput.x) > 0.01f)
//        {
//            float turn = movementInput.x * rotationSpeed * Time.fixedDeltaTime;
//            Quaternion deltaRot = Quaternion.Euler(0f, turn, 0f);
//            rigidbodyComponent.MoveRotation(rigidbodyComponent.rotation * deltaRot);
//        }

//        // 2) Gå frem/bagud i den retning pandaen peger (transform.forward)
//        Vector3 velocity = transform.forward * (movementInput.z * movementSpeed);
//        velocity.y = rigidbodyComponent.linearVelocity.y; // behold hop/tyngdekraft
//        rigidbodyComponent.linearVelocity = velocity;
//    }
//}

//
//------------Buttens--------------------------------------------------------------
//
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Animator))]
public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float movementSpeed = 5f;
    [SerializeField] private float rotationSpeed = 20f;
    [SerializeField] private float inputDeadzone = 0.05f;
    [SerializeField] private float walkThreshold = 0.0001f;

    private Rigidbody rb;
    private Animator anim;
    private Vector3 movementInput;
    private bool hasActionInput;   // Fik vi input via Input System i denne frame?

    // --- properties som dine tests bruger ---
    public float MovementSpeed
    {
        get => movementSpeed;
        set => movementSpeed = value;
    }

    public Rigidbody RigidbodyComponent
    {
        get => rb;
        set => rb = value;
    }
    // ----------------------------------------

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        anim = GetComponent<Animator>();
    }

    // KALDES AF PlayerInput -> MoveAndTurn (CallbackContext)
    public void OnMoveAndTurn(InputAction.CallbackContext ctx)
    {
        Vector2 v = ctx.ReadValue<Vector2>();

        // når alle knapper slippes, får vi "canceled"
        if (ctx.canceled)
        {
            movementInput = Vector3.zero;
            hasActionInput = false;   // <-- her slår vi det FRA
            return;
        }

        if (Mathf.Abs(v.x) < inputDeadzone) v.x = 0f;
        if (Mathf.Abs(v.y) < inputDeadzone) v.y = 0f;

        // x = drej, y = frem/tilbage
        movementInput = new Vector3(v.x, 0f, v.y);
        hasActionInput = movementInput.sqrMagnitude > 0f; // <-- her slår vi det TIL
    }

    // Hjælpemetode som tests kan kalde direkte
    public void SetMovementInput(Vector3 input)
    {
        movementInput = input;
        hasActionInput = true;
    }

    private void Update()
    {
        // Fallback: tastatur hvis der ikke er kommet input via actions
        if (!hasActionInput)
        {
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");
            movementInput = new Vector3(h, 0f, v);

            if (Mathf.Abs(movementInput.x) < inputDeadzone) movementInput.x = 0f;
            if (Mathf.Abs(movementInput.z) < inputDeadzone) movementInput.z = 0f;
        }

        bool isWalking = Mathf.Abs(movementInput.z) > walkThreshold;
        anim.SetBool("IsWalking", isWalking);

        // INGEN reset her!
    }

    private void FixedUpdate()
    {
        ApplyMovement();
    }

    // Metode som tests (og FixedUpdate) kalder
    public void ApplyMovement()
    {
        if (rb == null) return;

        // rotation
        if (Mathf.Abs(movementInput.x) > 0.01f)
        {
            float turn = movementInput.x * rotationSpeed * Time.fixedDeltaTime;
            rb.MoveRotation(rb.rotation * Quaternion.Euler(0f, turn, 0f));
        }

        // frem/tilbage
        Vector3 vel = transform.forward * (movementInput.z * movementSpeed);
        vel.y = rb.linearVelocity.y;
        rb.linearVelocity = vel;
    }
}
