using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Animator))]

public class PlayerMovement : MonoBehaviour
{
    [SerializeField]
    private float movementSpeed = 5f;
    private float walkThreshold = 0.0001f;
    private float inputDeadzone = 0.05f;
    private float rotationSpeed = 180f; // unused for player yaw now, kept for tuning if needed

    [Header("Optional Camera (for camera-relative movement and pitch)")]
    [SerializeField]
    private Transform cameraTransform;
    [SerializeField]
    private float lookSensitivity = 90f; // degrees per second for pitch/yaw
    [SerializeField]
    private float maxPitch = 60f;

    private Rigidbody rigidbodyComponent;
    private Animator animator;
    private Vector3 movementInput;
    private Vector2 moveInput2D;
    private Vector2 lookInput2D;
    private float currentPitch = 0f;
    private float currentYaw = 0f;

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

    private void Awake()
    {
        if (rigidbodyComponent == null)
            rigidbodyComponent = GetComponent<Rigidbody>();

        if (animator == null)
            animator = GetComponent<Animator>();

        // Auto-assign main camera if nothing is set in inspector
        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
            Debug.Log("PlayerMovement: cameraTransform was null — assigned Camera.main. " +
                      "If you use Cinemachine, create a CameraRig (empty GameObject), set the vCam's Follow to it, " +
                      "and assign that rig here so Cinemachine doesn't override runtime rotation.", this);
        }

        if (cameraTransform != null)
        {
            // initialize yaw/pitch from current camera rotation
            currentYaw = cameraTransform.eulerAngles.y;
            currentPitch = cameraTransform.localEulerAngles.x;
            if (currentPitch > 180f) currentPitch -= 360f; // convert to signed angle
        }
    }

    public void SetMovementInput(Vector3 input)
    {
        movementInput = input;
    }

    // UI / PlayerInput (Invoke Unity Events) friendly methods
    public void OnMove(Vector2 value) => moveInput2D = value;
    public void OnLook(Vector2 value) => lookInput2D = value;

    // InputAction.CallbackContext overloads (if using Send Messages)
    public void OnMove(InputAction.CallbackContext ctx)
    {
        if (ctx.phase == InputActionPhase.Performed || ctx.phase == InputActionPhase.Started)
            moveInput2D = ctx.ReadValue<Vector2>();
        else if (ctx.phase == InputActionPhase.Canceled)
            moveInput2D = Vector2.zero;
    }

    public void OnLook(InputAction.CallbackContext ctx)
    {
        if (ctx.phase == InputActionPhase.Performed || ctx.phase == InputActionPhase.Started)
            lookInput2D = ctx.ReadValue<Vector2>();
        else if (ctx.phase == InputActionPhase.Canceled)
            lookInput2D = Vector2.zero;
    }

    private void Update()
    {
        UpdateMovementInput();
        UpdateAnimationState();
        UpdateLook(); // camera yaw + pitch updated here (camera-only)
    }

    private void FixedUpdate()
    {
        ApplyMovement();
    }

    private void UpdateMovementInput()
    {
        if (moveInput2D.sqrMagnitude > (inputDeadzone * inputDeadzone))
        {
            Vector2 raw = moveInput2D;
            if (Mathf.Abs(raw.x) < inputDeadzone) raw.x = 0f;
            if (Mathf.Abs(raw.y) < inputDeadzone) raw.y = 0f;

            Vector3 forward;
            Vector3 right;

            if (cameraTransform != null)
            {
                forward = cameraTransform.forward;
                right = cameraTransform.right;
            }
            else
            {
                forward = Vector3.forward;
                right = Vector3.right;
            }

            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();

            Vector3 camRelative = (right * raw.x) + (forward * raw.y);
            movementInput = camRelative;
        }
        else
        {
            float horizontalInput = Input.GetAxisRaw("Horizontal");
            float verticalInput = Input.GetAxisRaw("Vertical");

            movementInput = new Vector3(horizontalInput, 0f, verticalInput);

            if (Mathf.Abs(movementInput.z) < inputDeadzone)
            {
                movementInput.z = 0f;
            }
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

    private void UpdateLook()
    {
        if (cameraTransform == null)
        {
            // Clear, short message so you know why look does nothing
            Debug.LogWarning("PlayerMovement.UpdateLook: cameraTransform is null. Assign Main Camera or a CameraRig in the inspector.", this);
            return;
        }

        // Right stick -> yaw/pitch applied to the assigned transform.
        // If your project uses Cinemachine: don't assign the Cinemachine Virtual Camera directly here,
        // instead assign a separate 'CameraRig' that the vCam follows. Rotating that rig will affect the vCam.
        float lookX = lookInput2D.x;
        float lookY = lookInput2D.y;

        if (Mathf.Abs(lookX) > inputDeadzone)
            currentYaw += lookX * lookSensitivity * Time.deltaTime;

        if (Mathf.Abs(lookY) > inputDeadzone)
            currentPitch -= lookY * lookSensitivity * Time.deltaTime;

        currentPitch = Mathf.Clamp(currentPitch, -maxPitch, maxPitch);

        cameraTransform.eulerAngles = new Vector3(currentPitch, currentYaw, 0f);
    }

    public void ApplyMovement()
    {
        // No player yaw from right stick anymore — right stick controls camera only.

        Vector3 planar = movementInput;
        planar.y = 0f;

        Vector3 velocity = planar.normalized * (planar.magnitude * movementSpeed);
        velocity.y = rigidbodyComponent.linearVelocity.y;
        rigidbodyComponent.linearVelocity = velocity;
    }
}